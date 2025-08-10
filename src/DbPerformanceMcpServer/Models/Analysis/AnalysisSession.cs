using DbPerformanceMcpServer.Models.Optimization;

namespace DbPerformanceMcpServer.Models.Analysis;

/// <summary>
/// 分析セッション（読み取り専用、提案のみ）
/// </summary>
public class AnalysisSession
{
    /// <summary>
    /// セッションID
    /// </summary>
    public string SessionId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// 対象ビュー名
    /// </summary>
    public string ViewName { get; set; } = string.Empty;

    /// <summary>
    /// スナップショット保存パス
    /// </summary>
    public string SnapshotBasePath { get; set; } = string.Empty;

    /// <summary>
    /// ベースライン分析結果
    /// </summary>
    public ViewAnalysisResult? BaselineAnalysis { get; set; }

    /// <summary>
    /// 生成された最適化提案リスト
    /// </summary>
    public List<OptimizationProposal> GeneratedProposals { get; set; } = new();

    /// <summary>
    /// セッション開始時刻
    /// </summary>
    public DateTime StartTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// セッション終了時刻
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// 最大提案数
    /// </summary>
    public int MaxProposals { get; set; } = 10;

    /// <summary>
    /// 現在の分析状態
    /// </summary>
    public AnalysisSessionState State { get; set; } = AnalysisSessionState.Initializing;

    /// <summary>
    /// 分析完了フラグ
    /// </summary>
    public bool IsCompleted => State == AnalysisSessionState.Completed;

    /// <summary>
    /// エラーメッセージ（失敗時）
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 最終レポート内容
    /// </summary>
    public string? FinalReport { get; set; }
}

/// <summary>
/// 分析セッション状態
/// </summary>
public enum AnalysisSessionState
{
    Initializing,
    AnalyzingBaseline,
    GeneratingProposals,
    CreatingReports,
    Completed,
    Failed
}