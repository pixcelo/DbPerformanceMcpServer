namespace DbPerformanceMcpServer.Models.Analysis;

/// <summary>
/// パフォーマンス測定メトリクス
/// </summary>
public class PerformanceMetrics
{
    /// <summary>
    /// 実行時間（ミリ秒）
    /// </summary>
    public long ExecutionTimeMs { get; set; }

    /// <summary>
    /// CPU時間（ミリ秒）
    /// </summary>
    public long CpuTimeMs { get; set; }

    /// <summary>
    /// 論理読み取り数
    /// </summary>
    public long LogicalReads { get; set; }

    /// <summary>
    /// 物理読み取り数
    /// </summary>
    public long PhysicalReads { get; set; }

    /// <summary>
    /// 先読み読み取り数
    /// </summary>
    public long ReadAheadReads { get; set; }

    /// <summary>
    /// スキャン回数
    /// </summary>
    public int ScanCount { get; set; }

    /// <summary>
    /// 測定タイムスタンプ
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 測定実行回数
    /// </summary>
    public int MeasurementRuns { get; set; } = 1;

    /// <summary>
    /// 実行時間の標準偏差（複数回測定時）
    /// </summary>
    public double? ExecutionTimeStdDev { get; set; }

    /// <summary>
    /// パフォーマンス改善率（%）
    /// </summary>
    public double? ImprovementPercentage { get; set; }
}