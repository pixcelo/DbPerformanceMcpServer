namespace DbPerformanceMcpServer.Models.Analysis;

/// <summary>
/// 実行プラン分析結果
/// </summary>
public class ExecutionPlanAnalysis
{
    /// <summary>
    /// 実行プランXML
    /// </summary>
    public string ExecutionPlanXml { get; set; } = string.Empty;

    /// <summary>
    /// 特定された問題点
    /// </summary>
    public List<PerformanceIssue> IdentifiedIssues { get; set; } = new();

    /// <summary>
    /// 高コスト操作（30%以上のコスト）
    /// </summary>
    public List<HighCostOperation> HighCostOperations { get; set; } = new();

    /// <summary>
    /// カーディナリティ推定エラー
    /// </summary>
    public List<CardinalityEstimationError> CardinalityErrors { get; set; } = new();

    /// <summary>
    /// 暗黙の型変換
    /// </summary>
    public List<ImplicitConversion> ImplicitConversions { get; set; } = new();

    /// <summary>
    /// 総実行コスト
    /// </summary>
    public double TotalExecutionCost { get; set; }

    /// <summary>
    /// 分析完了フラグ
    /// </summary>
    public bool AnalysisCompleted { get; set; }

    /// <summary>
    /// 総実行コスト（互換性のため）
    /// </summary>
    public double TotalCost 
    { 
        get => TotalExecutionCost; 
        set => TotalExecutionCost = value; 
    }

    /// <summary>
    /// テーブルスキャンの有無
    /// </summary>
    public bool HasTableScans { get; set; }

    /// <summary>
    /// インデックススキャンの有無
    /// </summary>
    public bool HasIndexScans { get; set; }

    /// <summary>
    /// ネストループ結合の有無
    /// </summary>
    public bool HasNestedLoops { get; set; }

    /// <summary>
    /// ハッシュ結合の有無
    /// </summary>
    public bool HasHashJoins { get; set; }

    /// <summary>
    /// マージ結合の有無
    /// </summary>
    public bool HasMergeJoins { get; set; }
}

/// <summary>
/// パフォーマンス問題
/// </summary>
public class PerformanceIssue
{
    /// <summary>
    /// 優先度（1が最高）
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// 問題種別
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 問題説明
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 影響を受けるオブジェクト
    /// </summary>
    public string AffectedObject { get; set; } = string.Empty;

    /// <summary>
    /// 推定影響度
    /// </summary>
    public string EstimatedImpact { get; set; } = string.Empty;

    /// <summary>
    /// 推奨アクション
    /// </summary>
    public string SuggestedAction { get; set; } = string.Empty;
}

/// <summary>
/// 高コスト操作
/// </summary>
public class HighCostOperation
{
    /// <summary>
    /// 操作種別
    /// </summary>
    public string OperationType { get; set; } = string.Empty;

    /// <summary>
    /// コスト割合（%）
    /// </summary>
    public double CostPercentage { get; set; }

    /// <summary>
    /// 対象オブジェクト
    /// </summary>
    public string TargetObject { get; set; } = string.Empty;
}

/// <summary>
/// カーディナリティ推定エラー
/// </summary>
public class CardinalityEstimationError
{
    /// <summary>
    /// 推定行数
    /// </summary>
    public long EstimatedRows { get; set; }

    /// <summary>
    /// 実際の行数
    /// </summary>
    public long ActualRows { get; set; }

    /// <summary>
    /// 乖離率
    /// </summary>
    public double DivergenceRatio { get; set; }

    /// <summary>
    /// 対象テーブル
    /// </summary>
    public string TargetTable { get; set; } = string.Empty;
}

/// <summary>
/// 暗黙の型変換
/// </summary>
public class ImplicitConversion
{
    /// <summary>
    /// 変換箇所
    /// </summary>
    public string ConversionLocation { get; set; } = string.Empty;

    /// <summary>
    /// 変換内容
    /// </summary>
    public string ConversionExpression { get; set; } = string.Empty;

    /// <summary>
    /// 対象カラム
    /// </summary>
    public string TargetColumn { get; set; } = string.Empty;
}