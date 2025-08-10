using DbPerformanceMcpServer.Models.Analysis;
using DbPerformanceMcpServer.Models.Optimization;

namespace DbPerformanceMcpServer.Services;

/// <summary>
/// 最適化オーケストレータ（全体プロセス制御）
/// </summary>
public interface IOptimizationOrchestrator
{
    /// <summary>
    /// ビューの包括的分析と最適化提案生成（実行はしない）
    /// </summary>
    Task<AnalysisSession> AnalyzeAndProposeOptimizationsAsync(
        string viewIdentifier, 
        int? maxProposals = null, 
        string? snapshotBasePath = null, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 最適化セッションを開始
    /// </summary>
    Task<OptimizationSession> StartOptimizationSessionAsync(string viewIdentifier, string? snapshotBasePath = null);

    /// <summary>
    /// 次の最適化ステップを実行
    /// </summary>
    Task<OptimizationSnapshot> ExecuteNextOptimizationStepAsync(OptimizationSession session);

    /// <summary>
    /// 最適化セッションを終了してレポート生成
    /// </summary>
    Task<string> FinalizeOptimizationSessionAsync(OptimizationSession session);

    /// <summary>
    /// セッションの現在の状況を取得
    /// </summary>
    Task<OptimizationSessionStatus> GetSessionStatusAsync(OptimizationSession session);
}

/// <summary>
/// 最適化セッション
/// </summary>
public class OptimizationSession
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
    /// 実行された最適化ステップ
    /// </summary>
    public List<OptimizationSnapshot> ExecutedSteps { get; set; } = new();

    /// <summary>
    /// 残りの最適化候補
    /// </summary>
    public Queue<OptimizationSuggestion> PendingSuggestions { get; set; } = new();

    /// <summary>
    /// セッション開始時刻
    /// </summary>
    public DateTime StartTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// セッション終了時刻
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// 現在のセッション状態
    /// </summary>
    public OptimizationSessionState State { get; set; } = OptimizationSessionState.Initializing;

    /// <summary>
    /// 最大ステップ数
    /// </summary>
    public int MaxSteps { get; set; } = 10;

    /// <summary>
    /// 現在のステップ番号
    /// </summary>
    public int CurrentStepNumber => ExecutedSteps.Count + 1;

    /// <summary>
    /// より多くの最適化候補があるか
    /// </summary>
    public bool HasMoreOptimizations => PendingSuggestions.Count > 0 && CurrentStepNumber <= MaxSteps;
}

/// <summary>
/// 最適化セッション状態
/// </summary>
public enum OptimizationSessionState
{
    Initializing,
    AnalyzingBaseline,
    ExecutingOptimizations,
    GeneratingReport,
    Completed,
    Failed
}

/// <summary>
/// セッション状況
/// </summary>
public class OptimizationSessionStatus
{
    /// <summary>
    /// 現在の状態
    /// </summary>
    public OptimizationSessionState State { get; set; }

    /// <summary>
    /// 進捗率（%）
    /// </summary>
    public double ProgressPercentage { get; set; }

    /// <summary>
    /// 実行済みステップ数
    /// </summary>
    public int CompletedSteps { get; set; }

    /// <summary>
    /// 成功したステップ数
    /// </summary>
    public int SuccessfulSteps { get; set; }

    /// <summary>
    /// 総改善率（%）
    /// </summary>
    public double? TotalImprovementPercentage { get; set; }

    /// <summary>
    /// 現在の処理内容
    /// </summary>
    public string CurrentActivity { get; set; } = string.Empty;

    /// <summary>
    /// エラーメッセージ（失敗時）
    /// </summary>
    public string? ErrorMessage { get; set; }
}