using DbPerformanceMcpServer.Models.Optimization;

namespace DbPerformanceMcpServer.Models.Analysis;

/// <summary>
/// 最適化提案
/// </summary>
public class OptimizationSuggestion
{
    /// <summary>
    /// 提案ID
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// 優先度（1が最高）
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// 最適化アクション種別
    /// </summary>
    public OptimizationActionType ActionType { get; set; }

    /// <summary>
    /// 提案名
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 詳細説明
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 対象オブジェクト（テーブル名、カラム名など）
    /// </summary>
    public string? TargetObject { get; set; }

    /// <summary>
    /// 推定改善効果
    /// </summary>
    public string EstimatedImpact { get; set; } = string.Empty;

    /// <summary>
    /// 実行予定SQL
    /// </summary>
    public string ExecutionSql { get; set; } = string.Empty;

    /// <summary>
    /// 実行時の注意点
    /// </summary>
    public string? ExecutionNotes { get; set; }

    /// <summary>
    /// 実行リスクレベル
    /// </summary>
    public RiskLevel RiskLevel { get; set; } = RiskLevel.Low;

    /// <summary>
    /// SQL Server 2016互換性
    /// </summary>
    public bool SQL2016Compatible { get; set; } = true;

    /// <summary>
    /// 推定リスク（文字列形式）
    /// </summary>
    public string EstimatedRisk 
    { 
        get => RiskLevel.ToString(); 
        set 
        { 
            if (Enum.TryParse<RiskLevel>(value, ignoreCase: true, out var risk))
                RiskLevel = risk;
        } 
    }
}

/// <summary>
/// リスクレベル
/// </summary>
public enum RiskLevel
{
    Low,      // 低リスク（統計更新など）
    Medium,   // 中リスク（ビュー定義変更など）
    High      // 高リスク（インデックス追加など）
}