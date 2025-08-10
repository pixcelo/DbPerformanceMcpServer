using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text.Json;

namespace DbPerformanceMcpServer.Services;

/// <summary>
/// SQL Server接続サービス実装
/// </summary>
public class SqlConnectionService : ISqlConnectionService
{
    private readonly string _connectionString;

    public SqlConnectionService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? "Server=localhost;Database=PMS;Integrated Security=true;TrustServerCertificate=true;"; // デフォルト値で回避
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
            
            return result != null && result.Equals(1);
        }
        catch (Exception)
        {
            return false;
        }
    }
}