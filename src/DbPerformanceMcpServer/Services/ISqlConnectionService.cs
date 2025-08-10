using Microsoft.Data.SqlClient;

namespace DbPerformanceMcpServer.Services;

/// <summary>
/// SQL Server接続サービス
/// </summary>
public interface ISqlConnectionService
{
    /// <summary>
    /// SQLクエリを実行して結果をJSON文字列で返却
    /// </summary>
    Task<string> ExecuteQueryAsync(string sql, CancellationToken cancellationToken = default);

    /// <summary>
    /// SQLクエリを実行してスカラー値を返却
    /// </summary>
    Task<T?> ExecuteScalarAsync<T>(string sql, CancellationToken cancellationToken = default);

    /// <summary>
    /// 非クエリSQL（INSERT, UPDATE, DELETE等）を実行
    /// </summary>
    Task<int> ExecuteNonQueryAsync(string sql, CancellationToken cancellationToken = default);

    /// <summary>
    /// 実行プランを含むクエリを実行
    /// </summary>
    Task<(string ResultJson, string ExecutionPlanXml)> ExecuteQueryWithPlanAsync(string viewName, CancellationToken cancellationToken = default);

    /// <summary>
    /// パフォーマンス統計情報を含むクエリを実行
    /// </summary>
    Task<(string ResultJson, string PerformanceStats)> ExecuteQueryWithStatsAsync(string viewName, int runs = 1, CancellationToken cancellationToken = default);

    /// <summary>
    /// 接続をテスト
    /// </summary>
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// ビュー定義を取得
    /// </summary>
    Task<string> GetViewDefinitionAsync(string viewName, CancellationToken cancellationToken = default);

    /// <summary>
    /// 結果セットのSHA2_256ハッシュを計算
    /// </summary>
    Task<string> CalculateResultHashAsync(string viewName, CancellationToken cancellationToken = default);

    /// <summary>
    /// 詳細なパフォーマンス測定（IO統計、時間統計、実行プランを含む）
    /// </summary>
    Task<PerformanceMeasurement> MeasureDetailedPerformanceAsync(string viewName, int runs = 3, CancellationToken cancellationToken = default);

    /// <summary>
    /// トランザクション内でSQLを実行（ロールバック可能）
    /// </summary>
    Task<T> ExecuteInTransactionAsync<T>(Func<SqlConnection, SqlTransaction, CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default);
}

/// <summary>
/// 詳細なパフォーマンス測定結果
/// </summary>
public class PerformanceMeasurement
{
    public long ExecutionTimeMs { get; set; }
    public long CpuTimeMs { get; set; }
    public long LogicalReads { get; set; }
    public long PhysicalReads { get; set; }
    public long ReadAheadReads { get; set; }
    public int ScanCount { get; set; }
    public int MeasurementRuns { get; set; }
    public double ExecutionTimeStdDev { get; set; }
    public DateTime Timestamp { get; set; }
    public string? ExecutionPlanXml { get; set; }
    public List<long> IndividualRunTimes { get; set; } = new();
}