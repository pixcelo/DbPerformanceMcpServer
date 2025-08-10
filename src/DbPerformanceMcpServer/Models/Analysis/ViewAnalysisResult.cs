namespace DbPerformanceMcpServer.Models.Analysis;

/// <summary>
/// ビュー分析結果
/// </summary>
public class ViewAnalysisResult
{
    /// <summary>
    /// ビュー名
    /// </summary>
    public string ViewName { get; set; } = string.Empty;

    /// <summary>
    /// ビュー定義SQL
    /// </summary>
    public string ViewDefinition { get; set; } = string.Empty;

    /// <summary>
    /// 結果セットのチェックサム
    /// </summary>
    public string ResultChecksum { get; set; } = string.Empty;

    /// <summary>
    /// ベースラインパフォーマンスメトリクス
    /// </summary>
    public PerformanceMetrics BaselineMetrics { get; set; } = new();

    /// <summary>
    /// 実行プラン分析結果
    /// </summary>
    public ExecutionPlanAnalysis ExecutionPlanAnalysis { get; set; } = new();

    /// <summary>
    /// 特定されたボトルネックと改善提案
    /// </summary>
    public List<OptimizationSuggestion> Suggestions { get; set; } = new();

    /// <summary>
    /// 分析実行日時
    /// </summary>
    public DateTime AnalysisTimestamp { get; set; } = DateTime.UtcNow;
}