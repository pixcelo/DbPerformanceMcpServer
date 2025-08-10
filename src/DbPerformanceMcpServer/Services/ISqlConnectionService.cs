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
}