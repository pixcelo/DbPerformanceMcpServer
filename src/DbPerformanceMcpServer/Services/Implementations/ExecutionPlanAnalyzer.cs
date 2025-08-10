using System.Xml;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using DbPerformanceMcpServer.Models.Analysis;
using DbPerformanceMcpServer.Models.Optimization;

namespace DbPerformanceMcpServer.Services;

/// <summary>
/// 実行プラン分析サービス実装
/// </summary>
public class ExecutionPlanAnalyzer : IExecutionPlanAnalyzer
{
    private readonly ILogger<ExecutionPlanAnalyzer> _logger;
    private const double HighCostThreshold = 0.30; // 30%以上を高コストと判定
    private const double CardinalityErrorThreshold = 10.0; // 10倍以上の乖離をエラーと判定

    public ExecutionPlanAnalyzer(ILogger<ExecutionPlanAnalyzer> logger)
    {
        _logger = logger;
    }

    public async Task<ExecutionPlanAnalysis> AnalyzeExecutionPlanAsync(string executionPlanXml)
    {
        await Task.CompletedTask; // 非同期の形式を保持
        
        try
        {
            if (string.IsNullOrWhiteSpace(executionPlanXml))
            {
                throw new ArgumentException("Execution plan XML cannot be null or empty", nameof(executionPlanXml));
            }

            _logger.LogDebug("Starting execution plan analysis, XML length: {Length}", executionPlanXml.Length);

            var analysis = new ExecutionPlanAnalysis
            {
                ExecutionPlanXml = executionPlanXml
            };

            var doc = XDocument.Parse(executionPlanXml);
            var ns = doc.Root?.GetDefaultNamespace();

            if (ns == null)
            {
                throw new InvalidOperationException("Could not determine XML namespace");
            }

            // 総コスト計算
            analysis.TotalExecutionCost = CalculateTotalCost(doc, ns);

            // 各種分析実行
            analysis.HighCostOperations = await ExtractHighCostOperationsAsync(executionPlanXml);
            analysis.CardinalityErrors = await DetectCardinalityErrorsAsync(executionPlanXml);
            analysis.ImplicitConversions = await DetectImplicitConversionsAsync(executionPlanXml);

            // 操作種別の検出
            AnalyzeOperationTypes(doc, ns, analysis);

            // 問題点の特定
            analysis.IdentifiedIssues = IdentifyPerformanceIssues(analysis);

            analysis.AnalysisCompleted = true;

            _logger.LogInformation("Execution plan analysis completed. Total cost: {TotalCost}, Issues: {IssueCount}, High-cost ops: {HighCostCount}",
                analysis.TotalExecutionCost, analysis.IdentifiedIssues.Count, analysis.HighCostOperations.Count);

            return analysis;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze execution plan");
            throw;
        }
    }

    public async Task<List<HighCostOperation>> ExtractHighCostOperationsAsync(string executionPlanXml)
    {
        await Task.CompletedTask;

        try
        {
            var operations = new List<HighCostOperation>();
            var doc = XDocument.Parse(executionPlanXml);
            var ns = doc.Root?.GetDefaultNamespace();

            if (ns == null) return operations;

            // RelOpノードを探索してコスト情報を抽出
            var relOpNodes = doc.Descendants(ns + "RelOp");
            var totalQueryCost = CalculateTotalCost(doc, ns);

            foreach (var node in relOpNodes)
            {
                var physicalOp = node.Attribute("PhysicalOp")?.Value ?? "";
                var estimatedTotalSubtreeCost = double.Parse(node.Attribute("EstimatedTotalSubtreeCost")?.Value ?? "0");
                var costPercentage = totalQueryCost > 0 ? (estimatedTotalSubtreeCost / totalQueryCost) * 100 : 0;

                if (costPercentage >= HighCostThreshold * 100) // 30%以上
                {
                    var targetObject = ExtractTargetObject(node, ns);

                    operations.Add(new HighCostOperation
                    {
                        OperationType = physicalOp,
                        CostPercentage = Math.Round(costPercentage, 2),
                        TargetObject = targetObject
                    });
                }
            }

            _logger.LogDebug("Extracted {Count} high-cost operations", operations.Count);
            return operations.OrderByDescending(op => op.CostPercentage).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract high-cost operations");
            return new List<HighCostOperation>();
        }
    }

    public async Task<List<CardinalityEstimationError>> DetectCardinalityErrorsAsync(string executionPlanXml)
    {
        await Task.CompletedTask;

        try
        {
            var errors = new List<CardinalityEstimationError>();
            var doc = XDocument.Parse(executionPlanXml);
            var ns = doc.Root?.GetDefaultNamespace();

            if (ns == null) return errors;

            var relOpNodes = doc.Descendants(ns + "RelOp");

            foreach (var node in relOpNodes)
            {
                var estimatedRows = long.Parse(node.Attribute("EstimateRows")?.Value ?? "0");
                var actualRows = long.Parse(node.Attribute("ActualRows")?.Value ?? "0");

                if (estimatedRows > 0 && actualRows > 0)
                {
                    var ratio = Math.Max(estimatedRows, actualRows) / (double)Math.Min(estimatedRows, actualRows);

                    if (ratio >= CardinalityErrorThreshold)
                    {
                        var targetTable = ExtractTargetTable(node, ns);

                        errors.Add(new CardinalityEstimationError
                        {
                            EstimatedRows = estimatedRows,
                            ActualRows = actualRows,
                            DivergenceRatio = Math.Round(ratio, 2),
                            TargetTable = targetTable
                        });
                    }
                }
            }

            _logger.LogDebug("Detected {Count} cardinality estimation errors", errors.Count);
            return errors.OrderByDescending(e => e.DivergenceRatio).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to detect cardinality estimation errors");
            return new List<CardinalityEstimationError>();
        }
    }

    public async Task<List<ImplicitConversion>> DetectImplicitConversionsAsync(string executionPlanXml)
    {
        await Task.CompletedTask;

        try
        {
            var conversions = new List<ImplicitConversion>();
            var doc = XDocument.Parse(executionPlanXml);
            var ns = doc.Root?.GetDefaultNamespace();

            if (ns == null) return conversions;

            // CONVERT_IMPLICITを探索
            var scalarOperators = doc.Descendants(ns + "ScalarOperator");

            foreach (var scalarOp in scalarOperators)
            {
                var intrinsic = scalarOp.Element(ns + "Intrinsic");
                if (intrinsic?.Attribute("FunctionName")?.Value == "CONVERT_IMPLICIT")
                {
                    var conversionExpr = scalarOp.ToString();
                    var location = FindConversionLocation(scalarOp, ns);
                    var targetColumn = ExtractTargetColumnFromConversion(scalarOp, ns);

                    conversions.Add(new ImplicitConversion
                    {
                        ConversionLocation = location,
                        ConversionExpression = conversionExpr,
                        TargetColumn = targetColumn
                    });
                }
            }

            _logger.LogDebug("Detected {Count} implicit conversions", conversions.Count);
            return conversions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to detect implicit conversions");
            return new List<ImplicitConversion>();
        }
    }

    public async Task<List<OptimizationSuggestion>> GenerateOptimizationSuggestionsAsync(ExecutionPlanAnalysis planAnalysis)
    {
        await Task.CompletedTask;

        try
        {
            var suggestions = new List<OptimizationSuggestion>();

            // 高コスト操作に基づく提案
            foreach (var operation in planAnalysis.HighCostOperations)
            {
                var suggestion = GenerateSuggestionForHighCostOperation(operation);
                if (suggestion != null)
                {
                    suggestions.Add(suggestion);
                }
            }

            // カーディナリティエラーに基づく提案
            foreach (var error in planAnalysis.CardinalityErrors)
            {
                suggestions.Add(new OptimizationSuggestion
                {
                    ActionType = OptimizationActionType.UpdateStatistics,
                    TargetObject = error.TargetTable,
                    Description = $"統計情報を更新してカーディナリティ推定を改善 (乖離率: {error.DivergenceRatio:F1}倍)",
                    EstimatedImpact = error.DivergenceRatio > 50 ? "高" : "中",
                    Priority = error.DivergenceRatio > 50 ? 1 : 2
                });
            }

            // 暗黙の型変換に基づく提案
            foreach (var conversion in planAnalysis.ImplicitConversions)
            {
                suggestions.Add(new OptimizationSuggestion
                {
                    ActionType = OptimizationActionType.FixImplicitConversion,
                    TargetObject = conversion.TargetColumn,
                    Description = $"暗黙の型変換を修正: {conversion.ConversionLocation}",
                    EstimatedImpact = "中",
                    Priority = 2
                });
            }

            // テーブルスキャンの改善提案
            if (planAnalysis.HasTableScans)
            {
                suggestions.Add(new OptimizationSuggestion
                {
                    ActionType = OptimizationActionType.OptimizeTableScans,
                    TargetObject = "テーブルスキャン操作",
                    Description = "テーブルスキャンを改善するため、適切なJOIN条件またはWHERE句の最適化を検討",
                    EstimatedImpact = "高",
                    Priority = 1
                });
            }

            _logger.LogInformation("Generated {Count} optimization suggestions", suggestions.Count);
            return suggestions.OrderBy(s => s.Priority).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate optimization suggestions");
            return new List<OptimizationSuggestion>();
        }
    }

    public async Task<ExecutionPlanComparison> CompareExecutionPlansAsync(string beforePlanXml, string afterPlanXml)
    {
        await Task.CompletedTask;

        try
        {
            var beforeAnalysis = await AnalyzeExecutionPlanAsync(beforePlanXml);
            var afterAnalysis = await AnalyzeExecutionPlanAsync(afterPlanXml);

            var comparison = new ExecutionPlanComparison();

            // コスト変化
            comparison.TotalCostChange = afterAnalysis.TotalExecutionCost - beforeAnalysis.TotalExecutionCost;

            // 改善・悪化した操作の比較
            CompareOperations(beforeAnalysis, afterAnalysis, comparison);

            // 全体評価
            comparison.OverallImprovement = EvaluateOverallImprovement(beforeAnalysis, afterAnalysis);

            _logger.LogInformation("Execution plan comparison completed. Cost change: {CostChange:F4}",
                comparison.TotalCostChange);

            return comparison;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to compare execution plans");
            throw;
        }
    }

    private double CalculateTotalCost(XDocument doc, XNamespace ns)
    {
        try
        {
            var statementNode = doc.Descendants(ns + "StmtSimple").FirstOrDefault();
            var totalCostStr = statementNode?.Attribute("StatementSubTreeCost")?.Value ?? "0";
            return double.Parse(totalCostStr);
        }
        catch
        {
            return 0.0;
        }
    }

    private void AnalyzeOperationTypes(XDocument doc, XNamespace ns, ExecutionPlanAnalysis analysis)
    {
        var relOpNodes = doc.Descendants(ns + "RelOp");

        foreach (var node in relOpNodes)
        {
            var physicalOp = node.Attribute("PhysicalOp")?.Value ?? "";

            switch (physicalOp.ToUpperInvariant())
            {
                case "TABLE SCAN":
                case "CLUSTERED INDEX SCAN":
                    analysis.HasTableScans = true;
                    break;
                case "INDEX SCAN":
                case "NONCLUSTERED INDEX SCAN":
                    analysis.HasIndexScans = true;
                    break;
                case "NESTED LOOPS":
                    analysis.HasNestedLoops = true;
                    break;
                case "HASH MATCH":
                    analysis.HasHashJoins = true;
                    break;
                case "MERGE JOIN":
                    analysis.HasMergeJoins = true;
                    break;
            }
        }
    }

    private List<PerformanceIssue> IdentifyPerformanceIssues(ExecutionPlanAnalysis analysis)
    {
        var issues = new List<PerformanceIssue>();

        // 高コスト操作の問題
        foreach (var op in analysis.HighCostOperations.Take(3)) // 上位3つのみ
        {
            issues.Add(new PerformanceIssue
            {
                Priority = 1,
                Type = "HighCostOperation",
                Description = $"{op.OperationType}操作が全体の{op.CostPercentage:F1}%のコストを占めています",
                AffectedObject = op.TargetObject,
                EstimatedImpact = op.CostPercentage > 50 ? "非常に高" : "高",
                SuggestedAction = GetSuggestedActionForOperation(op.OperationType)
            });
        }

        // カーディナリティエラーの問題
        foreach (var error in analysis.CardinalityErrors.Where(e => e.DivergenceRatio > 20).Take(2))
        {
            issues.Add(new PerformanceIssue
            {
                Priority = 2,
                Type = "CardinalityEstimationError",
                Description = $"カーディナリティ推定で{error.DivergenceRatio:F1}倍の乖離が発生",
                AffectedObject = error.TargetTable,
                EstimatedImpact = "中",
                SuggestedAction = "統計情報の更新"
            });
        }

        return issues.OrderBy(i => i.Priority).ToList();
    }

    private string ExtractTargetObject(XElement relOpNode, XNamespace ns)
    {
        try
        {
            var indexScan = relOpNode.Element(ns + "IndexScan");
            if (indexScan != null)
            {
                var objectNode = indexScan.Element(ns + "Object");
                if (objectNode != null)
                {
                    var schema = objectNode.Attribute("Schema")?.Value ?? "";
                    var table = objectNode.Attribute("Table")?.Value ?? "";
                    var index = objectNode.Attribute("Index")?.Value ?? "";
                    return string.IsNullOrEmpty(index) ? $"{schema}.{table}" : $"{schema}.{table}.{index}";
                }
            }

            var tableScan = relOpNode.Element(ns + "TableScan");
            if (tableScan != null)
            {
                var objectNode = tableScan.Element(ns + "Object");
                if (objectNode != null)
                {
                    var schema = objectNode.Attribute("Schema")?.Value ?? "";
                    var table = objectNode.Attribute("Table")?.Value ?? "";
                    return $"{schema}.{table}";
                }
            }

            return relOpNode.Attribute("PhysicalOp")?.Value ?? "Unknown";
        }
        catch
        {
            return "Unknown";
        }
    }

    private string ExtractTargetTable(XElement relOpNode, XNamespace ns)
    {
        try
        {
            var objectElements = relOpNode.Descendants(ns + "Object");
            var firstObject = objectElements.FirstOrDefault();
            if (firstObject != null)
            {
                var schema = firstObject.Attribute("Schema")?.Value ?? "";
                var table = firstObject.Attribute("Table")?.Value ?? "";
                return $"{schema}.{table}";
            }
            return "Unknown";
        }
        catch
        {
            return "Unknown";
        }
    }

    private string FindConversionLocation(XElement scalarOp, XNamespace ns)
    {
        try
        {
            var parentRelOp = scalarOp.Ancestors(ns + "RelOp").FirstOrDefault();
            return parentRelOp?.Attribute("PhysicalOp")?.Value ?? "Unknown";
        }
        catch
        {
            return "Unknown";
        }
    }

    private string ExtractTargetColumnFromConversion(XElement scalarOp, XNamespace ns)
    {
        try
        {
            var columnReference = scalarOp.Descendants(ns + "ColumnReference").FirstOrDefault();
            if (columnReference != null)
            {
                var table = columnReference.Attribute("Table")?.Value ?? "";
                var column = columnReference.Attribute("Column")?.Value ?? "";
                return $"{table}.{column}";
            }
            return "Unknown";
        }
        catch
        {
            return "Unknown";
        }
    }

    private OptimizationSuggestion? GenerateSuggestionForHighCostOperation(HighCostOperation operation)
    {
        return operation.OperationType.ToUpperInvariant() switch
        {
            "TABLE SCAN" => new OptimizationSuggestion
            {
                ActionType = OptimizationActionType.OptimizeTableScans,
                TargetObject = operation.TargetObject,
                Description = $"テーブルスキャンを最適化 (コスト: {operation.CostPercentage:F1}%)",
                EstimatedImpact = "高",
                Priority = 1
            },
            "CLUSTERED INDEX SCAN" => new OptimizationSuggestion
            {
                ActionType = OptimizationActionType.OptimizeTableScans, // インデックススキャンもテーブルスキャン最適化として扱う
                TargetObject = operation.TargetObject,
                Description = $"インデックススキャンを最適化 (コスト: {operation.CostPercentage:F1}%)",
                EstimatedImpact = "中",
                Priority = 2
            },
            "SORT" => new OptimizationSuggestion
            {
                ActionType = OptimizationActionType.RemoveUnnecessarySort,
                TargetObject = operation.TargetObject,
                Description = $"不要なソート操作を削除 (コスト: {operation.CostPercentage:F1}%)",
                EstimatedImpact = "中",
                Priority = 2
            },
            _ => null
        };
    }

    private void CompareOperations(ExecutionPlanAnalysis before, ExecutionPlanAnalysis after, ExecutionPlanComparison comparison)
    {
        // 簡略化された比較ロジック
        if (after.HasTableScans && !before.HasTableScans)
        {
            comparison.DegradedOperations.Add("新たなテーブルスキャンが発生");
        }
        else if (!after.HasTableScans && before.HasTableScans)
        {
            comparison.ImprovedOperations.Add("テーブルスキャンが削除されました");
        }

        if (after.HighCostOperations.Count < before.HighCostOperations.Count)
        {
            comparison.ImprovedOperations.Add("高コスト操作が減少");
        }
    }

    private string EvaluateOverallImprovement(ExecutionPlanAnalysis before, ExecutionPlanAnalysis after)
    {
        var costImprovement = (before.TotalExecutionCost - after.TotalExecutionCost) / before.TotalExecutionCost * 100;

        return costImprovement switch
        {
            > 20 => "大幅な改善",
            > 10 => "顕著な改善",
            > 5 => "軽微な改善",
            > -5 => "変化なし",
            _ => "性能悪化"
        };
    }

    private string GetSuggestedActionForOperation(string operationType)
    {
        return operationType.ToUpperInvariant() switch
        {
            "TABLE SCAN" => "WHERE句の最適化またはインデックス検討",
            "CLUSTERED INDEX SCAN" => "クエリの絞り込み条件を追加",
            "SORT" => "ORDER BYの必要性を確認",
            "HASH MATCH" => "JOIN条件を確認",
            _ => "クエリの最適化を検討"
        };
    }
}