using DbPerformanceMcpServer.Models.Analysis;
using DbPerformanceMcpServer.Models.Validation;

namespace DbPerformanceMcpServer.Models.Optimization;

/// <summary>
/// 最適化提案（実行はしない、提案のみ）
/// </summary>
public class OptimizationProposal
{
    /// <summary>
    /// 提案ID
    /// </summary>
    public string ProposalId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// アクション種別
    /// </summary>
    public OptimizationActionType ActionType { get; set; }

    /// <summary>
    /// 対象オブジェクト
    /// </summary>
    public string? TargetObject { get; set; }

    /// <summary>
    /// 生成された最適化SQL文（実行はしない）
    /// </summary>
    public string ProposedSql { get; set; } = string.Empty;

    /// <summary>
    /// 元のビュー定義（バックアップ用）
    /// </summary>
    public string OriginalViewDefinition { get; set; } = string.Empty;

    /// <summary>
    /// 改善予測効果
    /// </summary>
    public ExpectedImprovement ExpectedImprovement { get; set; } = new();

    /// <summary>
    /// リスク評価
    /// </summary>
    public RiskAssessment RiskAssessment { get; set; } = new();

    /// <summary>
    /// 実行手順書
    /// </summary>
    public ExecutionGuide ExecutionGuide { get; set; } = new();

    /// <summary>
    /// 提案生成日時
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 優先度（1が最高）
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// 提案の説明
    /// </summary>
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// 改善予測効果
/// </summary>
public class ExpectedImprovement
{
    /// <summary>
    /// 予測実行時間改善率（%）
    /// </summary>
    public double ExpectedExecutionTimeImprovementPercent { get; set; }

    /// <summary>
    /// 予測IO改善率（%）
    /// </summary>
    public double ExpectedIoImprovementPercent { get; set; }

    /// <summary>
    /// 影響度（低・中・高）
    /// </summary>
    public string ImpactLevel { get; set; } = "中";

    /// <summary>
    /// 改善予測の根拠
    /// </summary>
    public string ImprovementReason { get; set; } = string.Empty;
}

/// <summary>
/// リスク評価
/// </summary>
public class RiskAssessment
{
    /// <summary>
    /// リスクレベル（低・中・高）
    /// </summary>
    public string RiskLevel { get; set; } = "低";

    /// <summary>
    /// 潜在的リスク一覧
    /// </summary>
    public List<string> PotentialRisks { get; set; } = new();

    /// <summary>
    /// 回復手順
    /// </summary>
    public string RecoveryProcedure { get; set; } = string.Empty;

    /// <summary>
    /// 前提条件
    /// </summary>
    public List<string> Prerequisites { get; set; } = new();
}

/// <summary>
/// 実行手順書
/// </summary>
public class ExecutionGuide
{
    /// <summary>
    /// 実行前チェックリスト
    /// </summary>
    public List<string> PreExecutionChecklist { get; set; } = new();

    /// <summary>
    /// 実行手順
    /// </summary>
    public List<ExecutionStep> ExecutionSteps { get; set; } = new();

    /// <summary>
    /// 実行後検証手順
    /// </summary>
    public List<string> PostExecutionValidation { get; set; } = new();

    /// <summary>
    /// 推定実行時間
    /// </summary>
    public TimeSpan EstimatedExecutionTime { get; set; }
}

/// <summary>
/// 実行ステップ
/// </summary>
public class ExecutionStep
{
    /// <summary>
    /// ステップ番号
    /// </summary>
    public int StepNumber { get; set; }

    /// <summary>
    /// ステップ説明
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 実行SQL文
    /// </summary>
    public string SqlCommand { get; set; } = string.Empty;

    /// <summary>
    /// 期待される結果
    /// </summary>
    public string ExpectedResult { get; set; } = string.Empty;

    /// <summary>
    /// 注意事項
    /// </summary>
    public string? Notes { get; set; }
}