using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text.Json;
using System.Text;
using System.Security.Cryptography;

namespace DbPerformanceMcpServer.Services;

/// <summary>
/// SQL Server接続サービス実装
/// </summary>
public class SqlConnectionService : ISqlConnectionService
{
    private readonly string _connectionString;
    private readonly ILogger<SqlConnectionService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public SqlConnectionService(IConfiguration configuration, ILogger<SqlConnectionService> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("DefaultConnection connection string is required");
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions 
        { 
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        
        _logger.LogInformation("SqlConnectionService initialized with connection string configured");
    }

    public async Task<string> ExecuteQueryAsync(string sql, CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        
        using var command = new SqlCommand(sql, connection);
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        
        var results = new List<Dictionary<string, object?>>();
        
        while (await reader.ReadAsync(cancellationToken))
        {
            var row = new Dictionary<string, object?>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                var fieldName = reader.GetName(i);
                var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                row[fieldName] = value;
            }
            results.Add(row);
        }
        
        return JsonSerializer.Serialize(results, new JsonSerializerOptions 
        { 
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });
    }

    public async Task<T?> ExecuteScalarAsync<T>(string sql, CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        
        using var command = new SqlCommand(sql, connection);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        
        if (result == null || result == DBNull.Value)
            return default(T);
            
        return (T)Convert.ChangeType(result, typeof(T));
    }

    public async Task<int> ExecuteNonQueryAsync(string sql, CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        
        using var command = new SqlCommand(sql, connection);
        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<(string ResultJson, string ExecutionPlanXml)> ExecuteQueryWithPlanAsync(string viewName, CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        
        // 実行プラン取得を有効化
        await using var planCommand = new SqlCommand("SET SHOWPLAN_XML ON", connection);
        await planCommand.ExecuteNonQueryAsync(cancellationToken);
        
        // クエリ実行と実行プラン取得
        var sql = $"SELECT * FROM {viewName}";
        await using var command = new SqlCommand(sql, connection);
        
        var executionPlanXml = "";
        var resultJson = "";
        
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        
        // 最初の結果セット：実行プラン
        if (await reader.ReadAsync(cancellationToken))
        {
            executionPlanXml = reader.GetString(0);
        }
        
        // 次の結果セットに移動：実際のデータ
        if (await reader.NextResultAsync(cancellationToken))
        {
            var results = new List<Dictionary<string, object?>>();
            while (await reader.ReadAsync(cancellationToken))
            {
                var row = new Dictionary<string, object?>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var fieldName = reader.GetName(i);
                    var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    row[fieldName] = value;
                }
                results.Add(row);
            }
            
            resultJson = JsonSerializer.Serialize(results, new JsonSerializerOptions 
            { 
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
        }
        
        // 実行プラン取得を無効化
        await using var planOffCommand = new SqlCommand("SET SHOWPLAN_XML OFF", connection);
        await planOffCommand.ExecuteNonQueryAsync(cancellationToken);
        
        return (resultJson, executionPlanXml);
    }

    public async Task<(string ResultJson, string PerformanceStats)> ExecuteQueryWithStatsAsync(string viewName, int runs = 1, CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        
        var performanceResults = new List<Dictionary<string, object>>();
        var lastResultJson = "";
        
        for (int run = 1; run <= runs; run++)
        {
            // 統計情報を有効化
            await using var statsOnCommand = new SqlCommand("SET STATISTICS IO ON; SET STATISTICS TIME ON;", connection);
            await statsOnCommand.ExecuteNonQueryAsync(cancellationToken);
            
            var startTime = DateTime.UtcNow;
            
            // クエリ実行
            var sql = $"SELECT * FROM {viewName}";
            await using var command = new SqlCommand(sql, connection);
            
            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            
            // 結果セット読み込み（最後の実行のみ保存）
            if (run == runs)
            {
                var results = new List<Dictionary<string, object?>>();
                while (await reader.ReadAsync(cancellationToken))
                {
                    var row = new Dictionary<string, object?>();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        var fieldName = reader.GetName(i);
                        var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                        row[fieldName] = value;
                    }
                    results.Add(row);
                }
                
                lastResultJson = JsonSerializer.Serialize(results, new JsonSerializerOptions 
                { 
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });
            }
            else
            {
                // データを読み切って実行を完了
                while (await reader.ReadAsync(cancellationToken)) { }
            }
            
            var endTime = DateTime.UtcNow;
            var executionTimeMs = (long)(endTime - startTime).TotalMilliseconds;
            
            performanceResults.Add(new Dictionary<string, object>
            {
                ["run"] = run,
                ["execution_time_ms"] = executionTimeMs,
                ["timestamp"] = startTime.ToString("yyyy-MM-dd HH:mm:ss.fff")
            });
            
            // 統計情報を無効化
            await using var statsOffCommand = new SqlCommand("SET STATISTICS IO OFF; SET STATISTICS TIME OFF;", connection);
            await statsOffCommand.ExecuteNonQueryAsync(cancellationToken);
        }
        
        var performanceStats = JsonSerializer.Serialize(performanceResults, new JsonSerializerOptions 
        { 
            WriteIndented = true 
        });
        
        return (lastResultJson, performanceStats);
    }

    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            
            // 簡単なクエリで接続テスト
            await using var command = new SqlCommand("SELECT 1", connection);
            var result = await command.ExecuteScalarAsync(cancellationToken);
            
            _logger.LogDebug("Connection test successful");
            return result != null && result.Equals(1);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Connection test failed");
            return false;
        }
    }

    public async Task<string> GetViewDefinitionAsync(string viewName, CancellationToken cancellationToken = default)
    {
        try
        {
            var sql = @"
                SELECT OBJECT_DEFINITION(OBJECT_ID(@ViewName)) AS ViewDefinition";
            
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@ViewName", viewName);
            
            var result = await command.ExecuteScalarAsync(cancellationToken);
            var definition = result?.ToString() ?? string.Empty;
            
            if (string.IsNullOrWhiteSpace(definition))
            {
                throw new InvalidOperationException($"View '{viewName}' not found or definition is empty");
            }
            
            _logger.LogDebug("Retrieved view definition for {ViewName}, length: {Length}", viewName, definition.Length);
            return definition;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get view definition for {ViewName}", viewName);
            throw;
        }
    }

    public async Task<string> CalculateResultHashAsync(string viewName, CancellationToken cancellationToken = default)
    {
        try
        {
            var sql = $@"
                DECLARE @CheckSum NVARCHAR(MAX);
                SELECT @CheckSum = CONVERT(NVARCHAR(MAX), 
                    HASHBYTES('SHA2_256', 
                        (SELECT * FROM {viewName} ORDER BY (SELECT NULL) FOR XML RAW, BINARY BASE64)
                    ), 2);
                SELECT @CheckSum AS ResultChecksum;";
            
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            
            using var command = new SqlCommand(sql, connection);
            command.CommandTimeout = 300; // 5分のタイムアウト
            
            var result = await command.ExecuteScalarAsync(cancellationToken);
            var hash = result?.ToString() ?? string.Empty;
            
            if (string.IsNullOrWhiteSpace(hash))
            {
                throw new InvalidOperationException($"Failed to calculate hash for view '{viewName}'");
            }
            
            _logger.LogDebug("Calculated SHA2_256 hash for {ViewName}: {Hash}", viewName, hash[..8] + "...");
            return hash;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calculate result hash for {ViewName}", viewName);
            throw;
        }
    }

    public async Task<PerformanceMeasurement> MeasureDetailedPerformanceAsync(string viewName, int runs = 3, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            
            var measurement = new PerformanceMeasurement
            {
                MeasurementRuns = runs,
                Timestamp = DateTime.UtcNow
            };
            
            var executionTimes = new List<long>();
            long totalLogicalReads = 0;
            long totalPhysicalReads = 0;
            long totalReadAheadReads = 0;
            int totalScanCount = 0;
            
            for (int run = 1; run <= runs; run++)
            {
                // 統計をクリア
                await using var clearStatsCommand = new SqlCommand("DBCC DROPCLEANBUFFERS; DBCC FREEPROCCACHE;", connection);
                if (run == 1) // 最初の実行時のみ
                {
                    await clearStatsCommand.ExecuteNonQueryAsync(cancellationToken);
                }
                
                // 統計情報を有効化
                await using var statsOnCommand = new SqlCommand("SET STATISTICS IO ON; SET STATISTICS TIME ON;", connection);
                await statsOnCommand.ExecuteNonQueryAsync(cancellationToken);
                
                var startTime = DateTime.UtcNow;
                
                // クエリ実行
                var sql = $"SELECT * FROM {viewName}";
                await using var command = new SqlCommand(sql, connection);
                command.CommandTimeout = 300;
                
                using var reader = await command.ExecuteReaderAsync(cancellationToken);
                
                // データを読み切る
                int rowCount = 0;
                while (await reader.ReadAsync(cancellationToken))
                {
                    rowCount++;
                }
                
                var endTime = DateTime.UtcNow;
                var executionTime = (long)(endTime - startTime).TotalMilliseconds;
                executionTimes.Add(executionTime);
                
                _logger.LogDebug("Run {Run}/{TotalRuns} completed: {ExecutionTime}ms, {RowCount} rows", 
                    run, runs, executionTime, rowCount);
                
                // 統計情報を無効化
                await using var statsOffCommand = new SqlCommand("SET STATISTICS IO OFF; SET STATISTICS TIME OFF;", connection);
                await statsOffCommand.ExecuteNonQueryAsync(cancellationToken);
            }
            
            // 実行プランも取得（最後の実行で）
            try
            {
                var (_, executionPlanXml) = await ExecuteQueryWithPlanAsync(viewName, cancellationToken);
                measurement.ExecutionPlanXml = executionPlanXml;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to retrieve execution plan for {ViewName}", viewName);
            }
            
            // 統計を計算
            measurement.ExecutionTimeMs = (long)executionTimes.Average();
            measurement.ExecutionTimeStdDev = CalculateStandardDeviation(executionTimes);
            measurement.IndividualRunTimes = executionTimes;
            
            // IO統計は簡易版（実際はSET STATISTICS IOの出力をパースする必要がある）
            measurement.LogicalReads = totalLogicalReads / runs;
            measurement.PhysicalReads = totalPhysicalReads / runs;
            measurement.ReadAheadReads = totalReadAheadReads / runs;
            measurement.ScanCount = totalScanCount / runs;
            
            _logger.LogInformation("Performance measurement completed for {ViewName}: Avg={AvgMs}ms, StdDev={StdDev:F2}ms", 
                viewName, measurement.ExecutionTimeMs, measurement.ExecutionTimeStdDev);
            
            return measurement;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to measure performance for {ViewName}", viewName);
            throw;
        }
    }

    public async Task<T> ExecuteInTransactionAsync<T>(Func<SqlConnection, SqlTransaction, CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        
        using var transaction = connection.BeginTransaction();
        try
        {
            var result = await operation(connection, transaction, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Transaction rolled back due to error");
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static double CalculateStandardDeviation(List<long> values)
    {
        if (values.Count <= 1) return 0.0;
        
        var average = values.Average();
        var sumOfSquaresOfDifferences = values.Select(val => (val - average) * (val - average)).Sum();
        var standardDeviation = Math.Sqrt(sumOfSquaresOfDifferences / (values.Count - 1));
        return standardDeviation;
    }
}