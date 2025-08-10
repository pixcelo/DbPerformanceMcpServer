using DbPerformanceMcpServer.Models.Analysis;
using DbPerformanceMcpServer.Models.Optimization;
using DbPerformanceMcpServer.Models.Validation;

namespace DbPerformanceMcpServer.Services;

/// <summary>
/// 最適化実行サービス
/// </summary>
public interface IOptimizationService
{
    /// <summary>
    /// 単一の最適化提案を生成（実行はしない）
    /// </summary>
    Task<OptimizationProposal> GenerateOptimizationProposalAsync(
        string viewName, 
        OptimizationActionType actionType, 
        string? targetObject = null, 
        string? snapshotBasePath = null, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 結果の同一性を検証
    /// </summary>
    Task<ValidationResult> ValidateResultIntegrityAsync(string viewName, string baselineChecksum, CancellationToken cancellationToken = default);

    /// <summary>
    /// パフォーマンス測定
    /// </summary>
    Task<PerformanceMetrics> MeasurePerformanceAsync(string viewName, int? runs = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 結果チェックサムを計算
    /// </summary>
    Task<string> CalculateResultChecksumAsync(string viewName, CancellationToken cancellationToken = default);

    /// <summary>
    /// 最適化アクションのSQLを生成（実行はしない）
    /// </summary>
    Task<string> GenerateOptimizationSqlAsync(OptimizationActionType actionType, string viewName, string? targetObject = null);

    /// <summary>
    /// 実行手順書とリスク評価を生成
    /// </summary>
    Task<ExecutionGuide> GenerateExecutionGuideAsync(OptimizationActionType actionType, string viewName, string proposedSql, CancellationToken cancellationToken = default);

    /// <summary>
    /// 最終レポートを生成（フェーズ3）
    /// </summary>
    Task<string> GenerateFinalReportAsync(string viewName, string snapshotBasePath, CancellationToken cancellationToken = default);
}