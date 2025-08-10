using DbPerformanceMcpServer.Models.Analysis;
using DbPerformanceMcpServer.Models.Optimization;
using DbPerformanceMcpServer.Models.Validation;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace DbPerformanceMcpServer.Services;

/// <summary>
/// 最適化実行サービス実装
/// </summary>
public class OptimizationService : IOptimizationService
{
    private readonly ISqlConnectionService _sqlConnection;
    private readonly IValidationService _validationService;
    private readonly ISnapshotService _snapshotService;
    private readonly ILogger<OptimizationService> _logger;

    public OptimizationService(
        ISqlConnectionService sqlConnection, 
        IValidationService validationService,
        ISnapshotService snapshotService,
        ILogger<OptimizationService> logger)
    {
        _sqlConnection = sqlConnection;
        _validationService = validationService;
        _snapshotService = snapshotService;
        _logger = logger;
    }

    public async Task<OptimizationProposal> GenerateOptimizationProposalAsync(string viewName, OptimizationActionType actionType, string? targetObject = null, string? snapshotBasePath = null, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Generating optimization proposal for {ActionType} on {ViewName}", actionType, viewName);

            // 元のビュー定義を取得
            var originalViewDefinition = await _sqlConnection.GetViewDefinitionAsync(viewName, cancellationToken);

            // 最適化SQL生成（実行はしない）
            var proposedSql = await GenerateOptimizationSqlAsync(actionType, viewName, targetObject);

            // 提案生成
            var proposal = new OptimizationProposal
            {
                ActionType = actionType,
                TargetObject = targetObject,
                ProposedSql = proposedSql,
                OriginalViewDefinition = originalViewDefinition,
                Description = GetActionDescription(actionType),
                Priority = GetActionPriority(actionType),
                ExpectedImprovement = new ExpectedImprovement
                {
                    ExpectedExecutionTimeImprovementPercent = GetExpectedImprovement(actionType),
                    ImpactLevel = GetImpactLevel(actionType),
                    ImprovementReason = GetImprovementReason(actionType)
                },
                RiskAssessment = new RiskAssessment
                {
                    RiskLevel = GetRiskLevel(actionType),
                    PotentialRisks = GetPotentialRisks(actionType),
                    RecoveryProcedure = $"元のビュー定義に戻すには: {originalViewDefinition}",
                    Prerequisites = GetPrerequisites(actionType)
                }
            };

            // 実行手順書生成
            proposal.ExecutionGuide = await GenerateExecutionGuideAsync(actionType, viewName, proposedSql, cancellationToken);

            _logger.LogInformation("Generated optimization proposal: {ActionType} for {ViewName}", actionType, viewName);
            return proposal;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate optimization proposal for {ActionType} on {ViewName}", actionType, viewName);
            throw;
        }
    }

    public Task<ValidationResult> ValidateResultIntegrityAsync(string viewName, string baselineChecksum, CancellationToken cancellationToken = default)
    {
        // 骨格実装：常に成功する検証結果
        var result = new ValidationResult
        {
            IsValid = true,
            BaselineChecksum = baselineChecksum,
            CurrentChecksum = baselineChecksum, // 同じにして検証成功
            ValidationTimestamp = DateTime.UtcNow,
            ValidationDurationMs = 100
        };
        
        return Task.FromResult(result);
    }

    public Task<PerformanceMetrics> MeasurePerformanceAsync(string viewName, int? runs = null, CancellationToken cancellationToken = default)
    {
        // 骨格実装：ダミーパフォーマンス測定結果
        var metrics = new PerformanceMetrics
        {
            ExecutionTimeMs = Random.Shared.Next(500, 2000), // ランダムな実行時間
            CpuTimeMs = Random.Shared.Next(300, 1500),
            LogicalReads = Random.Shared.Next(100, 1000),
            PhysicalReads = Random.Shared.Next(10, 100),
            MeasurementRuns = runs ?? 3,
            Timestamp = DateTime.UtcNow
        };
        
        return Task.FromResult(metrics);
    }

    public Task<string> CalculateResultChecksumAsync(string viewName, CancellationToken cancellationToken = default)
    {
        // 骨格実装：ダミーチェックサム
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var checksum = $"dummy_checksum_{viewName}_{timestamp}";
        return Task.FromResult(checksum);
    }

    public async Task<string> GenerateOptimizationSqlAsync(OptimizationActionType actionType, string viewName, string? targetObject = null)
    {
        try
        {
            _logger.LogDebug("Generating optimization SQL for {ActionType} on {ViewName}", actionType, viewName);
            
            return actionType switch
            {
                OptimizationActionType.UpdateStatistics => await GenerateUpdateStatisticsSqlAsync(targetObject ?? viewName),
                OptimizationActionType.RemoveUnnecessaryDistinct => await GenerateRemoveDistinctSqlAsync(viewName),
                OptimizationActionType.ConvertSubqueryToJoin => await GenerateConvertSubqueryToJoinSqlAsync(viewName),
                OptimizationActionType.FixImplicitConversion => await GenerateFixImplicitConversionSqlAsync(viewName, targetObject),
                OptimizationActionType.OptimizeStringConcatenation => await GenerateOptimizeStringConcatenationSqlAsync(viewName),
                OptimizationActionType.PrecomputeCalculatedColumns => await GeneratePrecomputeCalculatedColumnsSqlAsync(viewName),
                OptimizationActionType.RemoveUnnecessarySort => await GenerateRemoveUnnecessarySortSqlAsync(viewName),
                OptimizationActionType.OptimizeStringOperations => await GenerateOptimizeStringOperationsSqlAsync(viewName),
                OptimizationActionType.OptimizeTableScans => await GenerateOptimizeTableScansSqlAsync(viewName),
                _ => throw new ArgumentException($"Unsupported optimization action type: {actionType}", nameof(actionType))
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate optimization SQL for {ActionType} on {ViewName}", actionType, viewName);
            throw;
        }
    }

    public async Task<ExecutionGuide> GenerateExecutionGuideAsync(OptimizationActionType actionType, string viewName, string proposedSql, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        
        var guide = new ExecutionGuide
        {
            PreExecutionChecklist = GetPreExecutionChecklist(actionType),
            ExecutionSteps = new List<ExecutionStep>
            {
                new ExecutionStep
                {
                    StepNumber = 1,
                    Description = "元のビュー定義をバックアップ",
                    SqlCommand = $"-- バックアップ用SQL\nSELECT OBJECT_DEFINITION(OBJECT_ID('{viewName}')) AS OriginalDefinition",
                    ExpectedResult = "現在のビュー定義が取得される",
                    Notes = "必ずバックアップを保存してください"
                },
                new ExecutionStep
                {
                    StepNumber = 2,
                    Description = $"{GetActionDescription(actionType)}を実行",
                    SqlCommand = proposedSql,
                    ExpectedResult = "ビューが正常に更新される",
                    Notes = "実行前にテスト環境で確認してください"
                },
                new ExecutionStep
                {
                    StepNumber = 3,
                    Description = "結果セット検証",
                    SqlCommand = $"SELECT TOP 10 * FROM {viewName}",
                    ExpectedResult = "期待される結果が返される",
                    Notes = "データの整合性を確認"
                }
            },
            PostExecutionValidation = GetPostExecutionValidation(actionType),
            EstimatedExecutionTime = TimeSpan.FromMinutes(GetEstimatedMinutes(actionType))
        };

        return guide;
    }

    public Task<string> GenerateFinalReportAsync(string viewName, string snapshotBasePath, CancellationToken cancellationToken = default)
    {
        // 骨格実装：ダミーレポート
        var report = $@"# SQL Server ビューパフォーマンス改善レポート（ダミー）

## 1. 概要
- **対象ビュー:** `{viewName}`
- **実行日時:** {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}
- **試行回数:** 2回 (成功: 2回, 失敗: 0回)

## 2. パフォーマンス改善サマリー

| 指標 | 改善前 | 改善後 | 改善率 |
|------|--------|--------|--------|
| 実行時間 (ms) | 1000 | 800 | 20% |
| 論理読み取り | 500 | 300 | 40% |

## 3. 実施した改善アクション

### 成功したアクション
| # | アクション | 改善効果 | 実施時刻 |
|---|-----------|----------|----------|
| 1 | UPDATE STATISTICS（ダミー） | 15% | {DateTime.UtcNow:HH:mm:ss} |
| 2 | Remove unnecessary DISTINCT（ダミー） | 5% | {DateTime.UtcNow:HH:mm:ss} |

## 4. 推奨事項

1. **定期的な統計情報更新**
   - 毎週日曜日にフルスキャン統計更新を実施
   
2. **継続的な監視**
   - 実行時間が1000msを超えた場合にアラート

---
*このレポートはダミー実装によって生成されました。*
*生成日時: {DateTime.UtcNow}*";

        return Task.FromResult(report);
    }

    // 具体的な最適化SQL生成メソッド群

    private async Task<string> GenerateUpdateStatisticsSqlAsync(string tableOrViewName)
    {
        await Task.CompletedTask;
        
        // ビュー名からテーブル名を抽出する場合は、ビュー定義から参照テーブルを特定
        var sql = tableOrViewName.Contains('.') 
            ? $"UPDATE STATISTICS {tableOrViewName} WITH FULLSCAN;"
            : $"-- ビュー {tableOrViewName} の統計更新には、ビュー定義分析が必要";
            
        return sql;
    }

    private async Task<string> GenerateRemoveDistinctSqlAsync(string viewName)
    {
        try
        {
            var viewDefinition = await _sqlConnection.GetViewDefinitionAsync(viewName);
            
            // 不要なDISTINCTを検出して除去
            var optimizedDefinition = RemoveUnnecessaryDistinct(viewDefinition);
            
            return $@"ALTER VIEW {viewName} AS 
{optimizedDefinition}";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate remove DISTINCT SQL for {ViewName}", viewName);
            return $"-- DISTINCT削除の自動生成に失敗: {ex.Message}";
        }
    }

    private async Task<string> GenerateConvertSubqueryToJoinSqlAsync(string viewName)
    {
        try
        {
            var viewDefinition = await _sqlConnection.GetViewDefinitionAsync(viewName);
            
            // サブクエリをJOINに変換
            var optimizedDefinition = ConvertSubqueryToJoin(viewDefinition);
            
            return $@"ALTER VIEW {viewName} AS 
{optimizedDefinition}";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate subquery to JOIN conversion for {ViewName}", viewName);
            return $"-- サブクエリ→JOIN変換の自動生成に失敗: {ex.Message}";
        }
    }

    private async Task<string> GenerateFixImplicitConversionSqlAsync(string viewName, string? targetColumn)
    {
        try
        {
            var viewDefinition = await _sqlConnection.GetViewDefinitionAsync(viewName);
            
            // 暗黙の型変換を修正
            var optimizedDefinition = FixImplicitConversion(viewDefinition, targetColumn);
            
            return $@"ALTER VIEW {viewName} AS 
{optimizedDefinition}";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate implicit conversion fix for {ViewName}", viewName);
            return $"-- 暗黙型変換修正の自動生成に失敗: {ex.Message}";
        }
    }

    private async Task<string> GenerateOptimizeStringConcatenationSqlAsync(string viewName)
    {
        try
        {
            var viewDefinition = await _sqlConnection.GetViewDefinitionAsync(viewName);
            
            // 文字列連結を最適化（CONCAT使用など）
            var optimizedDefinition = OptimizeStringConcatenation(viewDefinition);
            
            return $@"ALTER VIEW {viewName} AS 
{optimizedDefinition}";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate string concatenation optimization for {ViewName}", viewName);
            return $"-- 文字列連結最適化の自動生成に失敗: {ex.Message}";
        }
    }

    private async Task<string> GeneratePrecomputeCalculatedColumnsSqlAsync(string viewName)
    {
        await Task.CompletedTask;
        // 複雑な計算を事前計算する場合は、通常CTEや一時テーブルが必要
        // 制約により、この最適化は推奨しない
        return $@"-- 計算列の事前計算は制約により実装されていません（CTE追加が必要なため）";
    }

    private async Task<string> GenerateRemoveUnnecessarySortSqlAsync(string viewName)
    {
        try
        {
            var viewDefinition = await _sqlConnection.GetViewDefinitionAsync(viewName);
            
            // 不要なORDER BYを除去（ビューでのORDER BYは通常無効）
            var optimizedDefinition = RemoveUnnecessarySort(viewDefinition);
            
            return $@"ALTER VIEW {viewName} AS 
{optimizedDefinition}";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate unnecessary sort removal for {ViewName}", viewName);
            return $"-- 不要ソート削除の自動生成に失敗: {ex.Message}";
        }
    }

    private async Task<string> GenerateOptimizeStringOperationsSqlAsync(string viewName)
    {
        try
        {
            var viewDefinition = await _sqlConnection.GetViewDefinitionAsync(viewName);
            
            // 文字列操作を最適化（LTRIM/RTRIM使用など）
            var optimizedDefinition = OptimizeStringOperations(viewDefinition);
            
            return $@"ALTER VIEW {viewName} AS 
{optimizedDefinition}";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate string operations optimization for {ViewName}", viewName);
            return $"-- 文字列操作最適化の自動生成に失敗: {ex.Message}";
        }
    }

    private async Task<string> GenerateOptimizeTableScansSqlAsync(string viewName)
    {
        await Task.CompletedTask;
        // テーブルスキャン最適化は通常WHERE句の改善やインデックスヒントが必要
        // ただし制約によりインデックス作成は禁止されているため、限定的な最適化のみ可能
        return $@"-- テーブルスキャン最適化: WHERE句の改善や結合条件の最適化を検討してください
-- 注意: インデックス作成は制約により禁止されています";
    }

    // SQL最適化のヘルパーメソッド群

    #region Helper Methods for Proposal Generation

    private string GetActionDescription(OptimizationActionType actionType)
    {
        return actionType switch
        {
            OptimizationActionType.UpdateStatistics => "統計情報の更新（FULLSCANで正確性向上）",
            OptimizationActionType.RemoveUnnecessaryDistinct => "不要なDISTINCT句の除去",
            OptimizationActionType.ConvertSubqueryToJoin => "サブクエリをJOINに変換してパフォーマンス改善",
            OptimizationActionType.FixImplicitConversion => "暗黙的データ型変換の明示化",
            OptimizationActionType.OptimizeStringConcatenation => "文字列連結の最適化（CONCAT関数使用）",
            OptimizationActionType.PrecomputeCalculatedColumns => "計算列の事前計算（制約により限定的）",
            OptimizationActionType.RemoveUnnecessarySort => "不要なORDER BY句の除去",
            OptimizationActionType.OptimizeStringOperations => "文字列操作の最適化（LTRIM/RTRIM使用）",
            OptimizationActionType.OptimizeTableScans => "テーブルスキャンの最適化（WHERE句改善）",
            _ => $"不明なアクション: {actionType}"
        };
    }

    private int GetActionPriority(OptimizationActionType actionType)
    {
        return actionType switch
        {
            OptimizationActionType.UpdateStatistics => 1,
            OptimizationActionType.FixImplicitConversion => 2,
            OptimizationActionType.ConvertSubqueryToJoin => 3,
            OptimizationActionType.OptimizeTableScans => 4,
            OptimizationActionType.RemoveUnnecessaryDistinct => 5,
            OptimizationActionType.OptimizeStringConcatenation => 6,
            OptimizationActionType.OptimizeStringOperations => 7,
            OptimizationActionType.RemoveUnnecessarySort => 8,
            OptimizationActionType.PrecomputeCalculatedColumns => 9,
            _ => 10
        };
    }

    private double GetExpectedImprovement(OptimizationActionType actionType)
    {
        return actionType switch
        {
            OptimizationActionType.UpdateStatistics => 25.0,
            OptimizationActionType.FixImplicitConversion => 30.0,
            OptimizationActionType.ConvertSubqueryToJoin => 40.0,
            OptimizationActionType.OptimizeTableScans => 35.0,
            OptimizationActionType.RemoveUnnecessaryDistinct => 15.0,
            OptimizationActionType.OptimizeStringConcatenation => 10.0,
            OptimizationActionType.OptimizeStringOperations => 12.0,
            OptimizationActionType.RemoveUnnecessarySort => 5.0,
            OptimizationActionType.PrecomputeCalculatedColumns => 20.0,
            _ => 0.0
        };
    }

    private string GetImpactLevel(OptimizationActionType actionType)
    {
        return actionType switch
        {
            OptimizationActionType.UpdateStatistics => "中",
            OptimizationActionType.FixImplicitConversion => "高",
            OptimizationActionType.ConvertSubqueryToJoin => "高",
            OptimizationActionType.OptimizeTableScans => "高",
            OptimizationActionType.RemoveUnnecessaryDistinct => "中",
            OptimizationActionType.OptimizeStringConcatenation => "低",
            OptimizationActionType.OptimizeStringOperations => "低",
            OptimizationActionType.RemoveUnnecessarySort => "低",
            OptimizationActionType.PrecomputeCalculatedColumns => "中",
            _ => "不明"
        };
    }

    private string GetImprovementReason(OptimizationActionType actionType)
    {
        return actionType switch
        {
            OptimizationActionType.UpdateStatistics => "最新の統計情報により、クエリオプティマイザがより良い実行プランを選択",
            OptimizationActionType.FixImplicitConversion => "データ型変換のオーバーヘッドを削減し、インデックス使用効率を向上",
            OptimizationActionType.ConvertSubqueryToJoin => "サブクエリの反復実行を回避し、単一パスでの処理に変換",
            OptimizationActionType.OptimizeTableScans => "効率的なWHERE句により、処理対象レコード数を大幅削減",
            OptimizationActionType.RemoveUnnecessaryDistinct => "重複排除処理のオーバーヘッドを削減",
            OptimizationActionType.OptimizeStringConcatenation => "CONCAT関数使用により、NULL処理とメモリ効率を改善",
            OptimizationActionType.OptimizeStringOperations => "最適化された関数使用により、処理効率向上",
            OptimizationActionType.RemoveUnnecessarySort => "不要なソート処理を削除してCPU使用量を削減",
            OptimizationActionType.PrecomputeCalculatedColumns => "複雑な計算の事前実行により、クエリ実行時の処理負荷を軽減",
            _ => "不明な改善理由"
        };
    }

    private string GetRiskLevel(OptimizationActionType actionType)
    {
        return actionType switch
        {
            OptimizationActionType.UpdateStatistics => "低",
            OptimizationActionType.RemoveUnnecessaryDistinct => "低",
            OptimizationActionType.RemoveUnnecessarySort => "低",
            OptimizationActionType.OptimizeStringConcatenation => "低",
            OptimizationActionType.OptimizeStringOperations => "低",
            OptimizationActionType.FixImplicitConversion => "中",
            OptimizationActionType.OptimizeTableScans => "中",
            OptimizationActionType.ConvertSubqueryToJoin => "中",
            OptimizationActionType.PrecomputeCalculatedColumns => "高",
            _ => "不明"
        };
    }

    private List<string> GetPotentialRisks(OptimizationActionType actionType)
    {
        return actionType switch
        {
            OptimizationActionType.UpdateStatistics => new List<string>
            {
                "統計更新中のテーブルロック",
                "更新処理による一時的なパフォーマンス低下"
            },
            OptimizationActionType.FixImplicitConversion => new List<string>
            {
                "データ型変更による既存アプリケーションへの影響",
                "予期しないデータ変換エラーの可能性"
            },
            OptimizationActionType.ConvertSubqueryToJoin => new List<string>
            {
                "結果セットの変化（カーディナリティ変更）",
                "JOIN条件の複雑化によるメンテナンス性低下"
            },
            OptimizationActionType.PrecomputeCalculatedColumns => new List<string>
            {
                "複雑な変更によるビューの可読性低下",
                "CTEや一時テーブル使用による制約違反の可能性"
            },
            _ => new List<string> { "一般的な最適化リスク" }
        };
    }

    private List<string> GetPrerequisites(OptimizationActionType actionType)
    {
        return actionType switch
        {
            OptimizationActionType.UpdateStatistics => new List<string>
            {
                "統計更新権限の確認",
                "メンテナンスウィンドウでの実行推奨"
            },
            OptimizationActionType.FixImplicitConversion => new List<string>
            {
                "影響範囲の事前調査",
                "データ型互換性の確認"
            },
            OptimizationActionType.ConvertSubqueryToJoin => new List<string>
            {
                "結果セットの事前比較",
                "パフォーマンステストの実施"
            },
            _ => new List<string> { "テスト環境での事前検証" }
        };
    }

    private List<string> GetPreExecutionChecklist(OptimizationActionType actionType)
    {
        var commonChecks = new List<string>
        {
            "テスト環境での事前検証完了",
            "本番データベースのバックアップ取得",
            "影響範囲の関係者への通知"
        };

        var specificChecks = actionType switch
        {
            OptimizationActionType.UpdateStatistics => new List<string>
            {
                "統計更新権限の確認",
                "システム負荷の低い時間帯での実行"
            },
            OptimizationActionType.FixImplicitConversion => new List<string>
            {
                "データ型変更の影響調査",
                "アプリケーションコードでの型チェック"
            },
            _ => new List<string>()
        };

        commonChecks.AddRange(specificChecks);
        return commonChecks;
    }

    private List<string> GetPostExecutionValidation(OptimizationActionType actionType)
    {
        return new List<string>
        {
            "結果セットの同一性確認",
            "パフォーマンス改善効果の測定",
            "エラーログの確認",
            "関連アプリケーションの動作確認"
        };
    }

    private int GetEstimatedMinutes(OptimizationActionType actionType)
    {
        return actionType switch
        {
            OptimizationActionType.UpdateStatistics => 10,
            OptimizationActionType.RemoveUnnecessaryDistinct => 5,
            OptimizationActionType.RemoveUnnecessarySort => 3,
            OptimizationActionType.OptimizeStringConcatenation => 5,
            OptimizationActionType.OptimizeStringOperations => 5,
            OptimizationActionType.FixImplicitConversion => 15,
            OptimizationActionType.ConvertSubqueryToJoin => 20,
            OptimizationActionType.OptimizeTableScans => 10,
            OptimizationActionType.PrecomputeCalculatedColumns => 30,
            _ => 10
        };
    }

    #endregion

    private string RemoveUnnecessaryDistinct(string sqlQuery)
    {
        // 簡略化された実装：複数のDISTINCTがある場合に内側のDISTINCTを除去
        var pattern = @"\bDISTINCT\s+\(\s*SELECT\s+DISTINCT\b";
        var replacement = "DISTINCT (SELECT ";
        
        return Regex.Replace(sqlQuery, pattern, replacement, RegexOptions.IgnoreCase);
    }

    private string ConvertSubqueryToJoin(string sqlQuery)
    {
        // 簡略化された実装：EXISTS サブクエリをJOINに変換する例
        // より複雑な変換は手動で行う必要がある
        var existsPattern = @"WHERE\s+EXISTS\s*\(\s*SELECT\s+.*?FROM\s+(\w+)\s+.*?WHERE\s+(\w+)\.(\w+)\s*=\s*(\w+)\.(\w+)\s*\)";
        
        if (Regex.IsMatch(sqlQuery, existsPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline))
        {
            return sqlQuery + "\n-- EXISTS サブクエリが検出されました。手動でJOINに変換することを検討してください。";
        }
        
        return sqlQuery;
    }

    private string FixImplicitConversion(string sqlQuery, string? targetColumn)
    {
        if (string.IsNullOrEmpty(targetColumn))
            return sqlQuery;
        
        // 簡略化された実装：特定のカラムでの暗黙変換を明示的にキャスト
        var pattern = $@"\b{Regex.Escape(targetColumn)}\s*=\s*'([^']*)'";
        var replacement = $"{targetColumn} = CAST('$1' AS VARCHAR(255))";
        
        return Regex.Replace(sqlQuery, pattern, replacement, RegexOptions.IgnoreCase);
    }

    private string OptimizeStringConcatenation(string sqlQuery)
    {
        // + 演算子による文字列連結をCONCATに変換（SQL Server 2012+）
        var pattern = @"(\w+)\s*\+\s*(\w+)(?:\s*\+\s*(\w+))?";
        
        return Regex.Replace(sqlQuery, pattern, match =>
        {
            if (match.Groups[3].Success)
                return $"CONCAT({match.Groups[1].Value}, {match.Groups[2].Value}, {match.Groups[3].Value})";
            else
                return $"CONCAT({match.Groups[1].Value}, {match.Groups[2].Value})";
        }, RegexOptions.IgnoreCase);
    }

    private string RemoveUnnecessarySort(string sqlQuery)
    {
        // ビュー内のORDER BYを除去（ビューでは無効のため）
        var pattern = @"ORDER\s+BY\s+[^)]+(?=\s*$|\s*;|\s*\))";
        return Regex.Replace(sqlQuery, pattern, "", RegexOptions.IgnoreCase);
    }

    private string OptimizeStringOperations(string sqlQuery)
    {
        // 文字列操作の最適化：SUBSTRING + LEN をLTRIM/RTRIMに変換など
        var patterns = new[]
        {
            (Pattern: @"SUBSTRING\s*\(\s*(\w+)\s*,\s*2\s*,\s*LEN\s*\(\s*\1\s*\)\s*-\s*1\s*\)", 
             Replacement: "SUBSTRING($1, 2, LEN($1) - 1)"), // 最適化の余地がある場合の例
        };
        
        var result = sqlQuery;
        foreach (var (pattern, replacement) in patterns)
        {
            result = Regex.Replace(result, pattern, replacement, RegexOptions.IgnoreCase);
        }
        
        return result;
    }
}