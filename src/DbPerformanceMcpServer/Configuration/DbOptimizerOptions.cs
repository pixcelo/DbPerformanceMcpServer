namespace DbPerformanceMcpServer.Configuration;

/// <summary>
/// DBパフォーマンス最適化の設定オプション
/// </summary>
public class DbOptimizerOptions
{
    /// <summary>
    /// スナップショット保存ベースパス
    /// </summary>
    public string SnapshotBasePath { get; set; } = "./performance_snapshots/";

    /// <summary>
    /// 最大最適化ステップ数
    /// </summary>
    public int MaxOptimizationSteps { get; set; } = 10;

    /// <summary>
    /// 検証タイムアウト（秒）
    /// </summary>
    public int ValidationTimeoutSeconds { get; set; } = 300;

    /// <summary>
    /// パフォーマンス測定実行回数
    /// </summary>
    public int PerformanceMeasurementRuns { get; set; } = 3;

    /// <summary>
    /// SQL Server 2016互換モード
    /// </summary>
    public bool SQL2016Compatible { get; set; } = true;

    /// <summary>
    /// 詳細ログ有効化
    /// </summary>
    public bool EnableDetailedLogging { get; set; } = true;

    /// <summary>
    /// 測定間でバッファをクリアするか
    /// </summary>
    public bool BufferClearBetweenRuns { get; set; } = false;
}