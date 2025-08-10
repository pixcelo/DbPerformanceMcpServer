using DbPerformanceMcpServer.Models.Analysis;

namespace DbPerformanceMcpServer.Services;

/// <summary>
/// 実行プラン分析サービス実装
/// </summary>
public class ExecutionPlanAnalyzer : IExecutionPlanAnalyzer
{
    public Task<ExecutionPlanAnalysis> AnalyzeExecutionPlanAsync(string executionPlanXml)
    {
        // TODO: 実装
        throw new NotImplementedException();
    }

    public Task<List<HighCostOperation>> ExtractHighCostOperationsAsync(string executionPlanXml)
    {
        // TODO: 実装
        throw new NotImplementedException();
    }

    public Task<List<CardinalityEstimationError>> DetectCardinalityErrorsAsync(string executionPlanXml)
    {
        // TODO: 実装
        throw new NotImplementedException();
    }

    public Task<List<ImplicitConversion>> DetectImplicitConversionsAsync(string executionPlanXml)
    {
        // TODO: 実装
        throw new NotImplementedException();
    }

    public Task<List<OptimizationSuggestion>> GenerateOptimizationSuggestionsAsync(ExecutionPlanAnalysis planAnalysis)
    {
        // TODO: 実装
        throw new NotImplementedException();
    }

    public Task<ExecutionPlanComparison> CompareExecutionPlansAsync(string beforePlanXml, string afterPlanXml)
    {
        // TODO: 実装
        throw new NotImplementedException();
    }
}