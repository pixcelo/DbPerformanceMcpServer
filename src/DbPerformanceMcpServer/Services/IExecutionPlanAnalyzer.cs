using DbPerformanceMcpServer.Models.Analysis;

namespace DbPerformanceMcpServer.Services;

/// <summary>
/// 実行プラン分析サービス
/// </summary>
public interface IExecutionPlanAnalyzer
{
    /// <summary>
    /// 実行プランXMLを分析してボトルネックを特定
    /// </summary>
    Task<ExecutionPlanAnalysis> AnalyzeExecutionPlanAsync(string executionPlanXml);

    /// <summary>
    /// 高コスト操作を抽出（30%以上のコスト）
    /// </summary>
    Task<List<HighCostOperation>> ExtractHighCostOperationsAsync(string executionPlanXml);

    /// <summary>
    /// カーディナリティ推定エラーを検出
    /// </summary>
    Task<List<CardinalityEstimationError>> DetectCardinalityErrorsAsync(string executionPlanXml);

    /// <summary>
    /// 暗黙の型変換を検出
    /// </summary>
    Task<List<ImplicitConversion>> DetectImplicitConversionsAsync(string executionPlanXml);

    /// <summary>
    /// 分析結果から最適化提案を生成
    /// </summary>
    Task<List<OptimizationSuggestion>> GenerateOptimizationSuggestionsAsync(ExecutionPlanAnalysis planAnalysis);

    /// <summary>
    /// 実行プラン比較（改善前後）
    /// </summary>
    Task<ExecutionPlanComparison> CompareExecutionPlansAsync(string beforePlanXml, string afterPlanXml);
}

/// <summary>
/// 実行プラン比較結果
/// </summary>
public class ExecutionPlanComparison
{
    /// <summary>
    /// 総コストの変化
    /// </summary>
    public double TotalCostChange { get; set; }

    /// <summary>
    /// 改善された操作
    /// </summary>
    public List<string> ImprovedOperations { get; set; } = new();

    /// <summary>
    /// 悪化した操作
    /// </summary>
    public List<string> DegradedOperations { get; set; } = new();

    /// <summary>
    /// 全体的な改善評価
    /// </summary>
    public string OverallImprovement { get; set; } = string.Empty;
}