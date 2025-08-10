using DbPerformanceMcpServer.Models.Analysis;
using DbPerformanceMcpServer.Models.Validation;

namespace DbPerformanceMcpServer.Models.Optimization;

/// <summary>
/// 最適化ステップのスナップショット
/// </summary>
public class OptimizationSnapshot
{
    /// <summary>
    /// スナップショットID（例: "01", "02"...）
    /// </summary>
    public string SnapshotId { get; set; } = string.Empty;

    /// <summary>
    /// アクション名
    /// </summary>
    public string ActionName { get; set; } = string.Empty;

    /// <summary>
    /// 実行したSQL文
    /// </summary>
    public string ActionSql { get; set; } = string.Empty;

    /// <summary>
    /// 実行前のビュー定義（ビュー変更の場合）
    /// </summary>
    public string? ViewDefinitionBefore { get; set; }

    /// <summary>
    /// 実行後のビュー定義（ビュー変更の場合）
    /// </summary>
    public string? ViewDefinitionAfter { get; set; }

    /// <summary>
    /// 検証結果
    /// </summary>
    public ValidationResult ValidationResult { get; set; } = new();

    /// <summary>
    /// パフォーマンスメトリクス（検証成功時のみ）
    /// </summary>
    public PerformanceMetrics? PerformanceMetrics { get; set; }

    /// <summary>
    /// 最適化ステータス
    /// </summary>
    public OptimizationStatus Status { get; set; }

    /// <summary>
    /// エラーメッセージ（失敗時）
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 実行開始時刻
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// 実行終了時刻
    /// </summary>
    public DateTime EndTime { get; set; }

    /// <summary>
    /// 改善率（%）
    /// </summary>
    public double? ImprovementPercentage { get; set; }

    /// <summary>
    /// ベースライン実行時間（比較用）
    /// </summary>
    public long BaselineExecutionTimeMs { get; set; }

    /// <summary>
    /// 改善後実行時間
    /// </summary>
    public long? ImprovedExecutionTimeMs { get; set; }
}

/// <summary>
/// 最適化ステータス
/// </summary>
public enum OptimizationStatus
{
    /// <summary>
    /// 実行中
    /// </summary>
    Running,

    /// <summary>
    /// 成功
    /// </summary>
    Success,

    /// <summary>
    /// 失敗（検証エラー）
    /// </summary>
    FailedValidation,

    /// <summary>
    /// 失敗（実行エラー）
    /// </summary>
    FailedExecution,

    /// <summary>
    /// 失敗（改善効果不足）
    /// </summary>
    FailedInsufficientImprovement,

    /// <summary>
    /// ロールバック済み
    /// </summary>
    RolledBack
}