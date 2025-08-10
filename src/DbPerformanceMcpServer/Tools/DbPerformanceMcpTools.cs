using System.ComponentModel;
using ModelContextProtocol.Server;
using DbPerformanceMcpServer.Services;
using DbPerformanceMcpServer.Models.Optimization;
using System.Text.Json;

namespace DbPerformanceMcpServer.Tools;

/// <summary>
/// DBパフォーマンス最適化のMCPツールクラス
/// </summary>
internal class DbPerformanceMcpTools
{
    private readonly IViewAnalysisService _viewAnalysisService;
    private readonly IOptimizationService _optimizationService;
    private readonly IOptimizationOrchestrator _optimizationOrchestrator;

    public DbPerformanceMcpTools(
        IViewAnalysisService viewAnalysisService,
        IOptimizationService optimizationService,
        IOptimizationOrchestrator optimizationOrchestrator)
    {
        _viewAnalysisService = viewAnalysisService;
        _optimizationService = optimizationService;
        _optimizationOrchestrator = optimizationOrchestrator;
    }

    [McpServerTool]
    [Description("フェーズ1: ビューのベースライン分析を実行します（実行計画、パフォーマンス、結果チェックサムを取得）")]
    public async Task<string> AnalyzeViewBaseline(
        [Description("ビュー名 (例: dbo.V_SlowSalesReport) またはビュー定義ファイルパス (.sql)")] string viewIdentifier,
        [Description("スナップショット保存パス (デフォルト: ./performance_snapshots/)")] string? snapshotBasePath = null)
    {
        try
        {
            var result = await _viewAnalysisService.AnalyzeBaselineAsync(viewIdentifier, snapshotBasePath);
            return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new
            {
                Error = true,
                Message = ex.Message,
                ViewIdentifier = viewIdentifier
            }, new JsonSerializerOptions { WriteIndented = true });
        }
    }

    [McpServerTool]
    [Description("フェーズ2: 単一の改善アクションを実行・検証します（結果の同一性を確認してからパフォーマンスを測定）")]
    public async Task<string> ExecuteOptimizationStep(
        [Description("対象ビュー名")] string viewName,
        [Description("改善アクション種別")] OptimizationActionType actionType,
        [Description("対象テーブル/カラム等のパラメータ（例: \"dbo.Orders\" または \"ProductQuery\"）")] string? targetObject = null,
        [Description("スナップショットベースパス")] string? snapshotBasePath = null)
    {
        try
        {
            var result = await _optimizationService.ExecuteOptimizationStepAsync(
                viewName, actionType, targetObject, snapshotBasePath);
            return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new
            {
                Error = true,
                Message = ex.Message,
                ViewName = viewName,
                ActionType = actionType.ToString(),
                TargetObject = targetObject
            }, new JsonSerializerOptions { WriteIndented = true });
        }
    }

    [McpServerTool]
    [Description("フェーズ1-3の完全自動実行: ベースライン分析から段階的改善、最終レポート生成まで自動実行")]
    public async Task<string> OptimizeViewFully(
        [Description("ビュー名またはファイルパス")] string viewIdentifier,
        [Description("最大改善ステップ数 (デフォルト: 10)")] int? maxSteps = null,
        [Description("スナップショットベースパス")] string? snapshotBasePath = null)
    {
        try
        {
            var result = await _optimizationOrchestrator.OptimizeViewFullyAsync(
                viewIdentifier, maxSteps, snapshotBasePath);
            return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new
            {
                Error = true,
                Message = ex.Message,
                ViewIdentifier = viewIdentifier
            }, new JsonSerializerOptions { WriteIndented = true });
        }
    }

    [McpServerTool]
    [Description("フェーズ3: 最適化セッションの最終レポートを生成します")]
    public async Task<string> GenerateFinalReport(
        [Description("対象ビュー名")] string viewName,
        [Description("スナップショットベースパス")] string snapshotBasePath)
    {
        try
        {
            var result = await _optimizationService.GenerateFinalReportAsync(viewName, snapshotBasePath);
            return result; // レポートはMarkdown形式で返却
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new
            {
                Error = true,
                Message = ex.Message,
                ViewName = viewName
            }, new JsonSerializerOptions { WriteIndented = true });
        }
    }

    [McpServerTool]
    [Description("指定されたビューの現在のパフォーマンスを測定します（改善効果の確認用）")]
    public async Task<string> MeasureViewPerformance(
        [Description("対象ビュー名")] string viewName,
        [Description("測定実行回数 (デフォルト: 3)")] int? measurementRuns = null)
    {
        try
        {
            var result = await _optimizationService.MeasurePerformanceAsync(viewName, measurementRuns);
            return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new
            {
                Error = true,
                Message = ex.Message,
                ViewName = viewName
            }, new JsonSerializerOptions { WriteIndented = true });
        }
    }

    [McpServerTool]
    [Description("指定されたビューの結果チェックサムを計算します（データ整合性確認用）")]
    public async Task<string> ValidateViewResults(
        [Description("対象ビュー名")] string viewName,
        [Description("比較対象のベースラインチェックサム（省略時は単純にチェックサムを返却）")] string? baselineChecksum = null)
    {
        try
        {
            if (string.IsNullOrEmpty(baselineChecksum))
            {
                // チェックサムを計算して返却
                var checksum = await _optimizationService.CalculateResultChecksumAsync(viewName);
                return JsonSerializer.Serialize(new
                {
                    ViewName = viewName,
                    Checksum = checksum,
                    Timestamp = DateTime.UtcNow
                }, new JsonSerializerOptions { WriteIndented = true });
            }
            else
            {
                // ベースラインとの検証を実行
                var result = await _optimizationService.ValidateResultIntegrityAsync(viewName, baselineChecksum);
                return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
            }
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new
            {
                Error = true,
                Message = ex.Message,
                ViewName = viewName
            }, new JsonSerializerOptions { WriteIndented = true });
        }
    }
}