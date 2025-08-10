using DbPerformanceMcpServer.Models.Analysis;
using DbPerformanceMcpServer.Models.Optimization;
using Microsoft.Extensions.Logging;

namespace DbPerformanceMcpServer.Services;

/// <summary>
/// 最適化オーケストレータ実装（読み取り専用モード）
/// </summary>
public class OptimizationOrchestrator : IOptimizationOrchestrator
{
    private readonly IViewAnalysisService _viewAnalysisService;
    private readonly IOptimizationService _optimizationService;
    private readonly IExecutionPlanAnalyzer _executionPlanAnalyzer;
    private readonly ILogger<OptimizationOrchestrator> _logger;

    public OptimizationOrchestrator(
        IViewAnalysisService viewAnalysisService,
        IOptimizationService optimizationService,
        IExecutionPlanAnalyzer executionPlanAnalyzer,
        ILogger<OptimizationOrchestrator> logger)
    {
        _viewAnalysisService = viewAnalysisService;
        _optimizationService = optimizationService;
        _executionPlanAnalyzer = executionPlanAnalyzer;
        _logger = logger;
    }

    public async Task<AnalysisSession> AnalyzeAndProposeOptimizationsAsync(
        string viewIdentifier, 
        int? maxProposals = null, 
        string? snapshotBasePath = null, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("分析セッション開始: {ViewName}", viewIdentifier);
            
            var session = new AnalysisSession
            {
                ViewName = viewIdentifier,
                SnapshotBasePath = snapshotBasePath ?? "./performance_snapshots/",
                MaxProposals = maxProposals ?? 10,
                State = AnalysisSessionState.AnalyzingBaseline
            };

            // フェーズ1: ベースライン分析
            session.BaselineAnalysis = await _viewAnalysisService.AnalyzeBaselineAsync(
                viewIdentifier, snapshotBasePath, cancellationToken);
            
            session.State = AnalysisSessionState.GeneratingProposals;
            _logger.LogInformation("ベースライン分析完了: {ViewName}", viewIdentifier);

            // フェーズ2: 最適化提案生成
            var actionTypes = GetOptimizationCandidates(session.BaselineAnalysis);
            
            foreach (var actionType in actionTypes.Take(session.MaxProposals))
            {
                try
                {
                    var proposal = await _optimizationService.GenerateOptimizationProposalAsync(
                        viewIdentifier, actionType, null, snapshotBasePath, cancellationToken);
                    
                    session.GeneratedProposals.Add(proposal);
                    _logger.LogDebug("提案生成完了: {ActionType}", actionType);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "提案生成失敗: {ActionType}", actionType);
                }
            }

            // フェーズ3: レポート生成
            session.State = AnalysisSessionState.CreatingReports;
            session.FinalReport = await _optimizationService.GenerateFinalReportAsync(
                viewIdentifier, session.SnapshotBasePath, cancellationToken);

            session.State = AnalysisSessionState.Completed;
            session.EndTime = DateTime.UtcNow;
            
            _logger.LogInformation("分析セッション完了: {ViewName}, 提案数: {ProposalCount}", 
                viewIdentifier, session.GeneratedProposals.Count);
            
            return session;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "分析セッション失敗: {ViewName}", viewIdentifier);
            throw;
        }
    }

    private List<OptimizationActionType> GetOptimizationCandidates(ViewAnalysisResult? baselineAnalysis)
    {
        var candidates = new List<OptimizationActionType>();
        
        if (baselineAnalysis == null)
        {
            // デフォルトの基本的な最適化を提案
            return new List<OptimizationActionType>
            {
                OptimizationActionType.UpdateStatistics,
                OptimizationActionType.RemoveUnnecessaryDistinct,
                OptimizationActionType.ConvertSubqueryToJoin
            };
        }

        // 統計情報は常に更新候補
        candidates.Add(OptimizationActionType.UpdateStatistics);

        // 実行プランに基づいた最適化候補の選定
        if (baselineAnalysis.Suggestions?.Any() == true)
        {
            foreach (var suggestion in baselineAnalysis.Suggestions)
            {
                var actionType = MapSuggestionToActionType(suggestion.Description);
                if (actionType.HasValue && !candidates.Contains(actionType.Value))
                {
                    candidates.Add(actionType.Value);
                }
            }
        }

        // パフォーマンス指標に基づく追加候補
        if (baselineAnalysis.BaselineMetrics != null)
        {
            if (baselineAnalysis.BaselineMetrics.ExecutionTimeMs > 1000)
            {
                candidates.Add(OptimizationActionType.OptimizeTableScans);
                candidates.Add(OptimizationActionType.FixImplicitConversion);
            }
            
            if (baselineAnalysis.BaselineMetrics.LogicalReads > 500)
            {
                candidates.Add(OptimizationActionType.RemoveUnnecessaryDistinct);
            }
        }

        return candidates.Distinct().ToList();
    }

    private OptimizationActionType? MapSuggestionToActionType(string suggestion)
    {
        var lowerSuggestion = suggestion.ToLowerInvariant();
        
        if (lowerSuggestion.Contains("distinct"))
            return OptimizationActionType.RemoveUnnecessaryDistinct;
        if (lowerSuggestion.Contains("subquery") || lowerSuggestion.Contains("join"))
            return OptimizationActionType.ConvertSubqueryToJoin;
        if (lowerSuggestion.Contains("conversion") || lowerSuggestion.Contains("cast"))
            return OptimizationActionType.FixImplicitConversion;
        if (lowerSuggestion.Contains("statistics"))
            return OptimizationActionType.UpdateStatistics;
        if (lowerSuggestion.Contains("scan") || lowerSuggestion.Contains("seek"))
            return OptimizationActionType.OptimizeTableScans;
        if (lowerSuggestion.Contains("order by"))
            return OptimizationActionType.RemoveUnnecessarySort;
        
        return null;
    }

    public Task<OptimizationSession> StartOptimizationSessionAsync(string viewIdentifier, string? snapshotBasePath = null)
    {
        // 読み取り専用モードでは実際のセッション開始は非推奨
        // 代わりに AnalyzeAndProposeOptimizationsAsync を使用してください
        _logger.LogWarning("StartOptimizationSessionAsync は読み取り専用モードでは非推奨です。AnalyzeAndProposeOptimizationsAsync を使用してください。");
        
        var session = new OptimizationSession
        {
            ViewName = viewIdentifier,
            SnapshotBasePath = snapshotBasePath ?? "./performance_snapshots/",
            State = OptimizationSessionState.Completed, // 即座に完了状態
            EndTime = DateTime.UtcNow
        };
        
        return Task.FromResult(session);
    }

    public Task<OptimizationSnapshot> ExecuteNextOptimizationStepAsync(OptimizationSession session)
    {
        // 読み取り専用モードでは実行不可
        throw new InvalidOperationException("読み取り専用モードでは最適化の実行はできません。提案の生成のみ可能です。");
    }

    public Task<string> FinalizeOptimizationSessionAsync(OptimizationSession session)
    {
        // 読み取り専用モードでは実行不可
        throw new InvalidOperationException("読み取り専用モードでは最適化の実行はできません。提案の生成のみ可能です。");
    }

    public Task<OptimizationSessionStatus> GetSessionStatusAsync(OptimizationSession session)
    {
        // 読み取り専用モードでは常に完了状態
        var status = new OptimizationSessionStatus
        {
            State = OptimizationSessionState.Completed,
            ProgressPercentage = 100.0,
            CompletedSteps = 0,
            SuccessfulSteps = 0,
            CurrentActivity = "読み取り専用モード: 実行は行われません"
        };
        
        return Task.FromResult(status);
    }
}