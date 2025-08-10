using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using DbPerformanceMcpServer.Configuration;
using DbPerformanceMcpServer.Models.Optimization;

namespace DbPerformanceMcpServer.Services.Implementations;

/// <summary>
/// 最適化制約の検証サービス実装
/// </summary>
public class ConstraintValidator : IConstraintValidator
{
    private readonly OptimizationConstraints _constraints;
    private readonly ILogger<ConstraintValidator> _logger;

    public ConstraintValidator(IOptions<DbOptimizerOptions> options, ILogger<ConstraintValidator> logger)
    {
        _constraints = options.Value.Constraints;
        _logger = logger;
    }

    public async Task<ConstraintValidationResult> ValidateActionAsync(OptimizationActionType actionType)
    {
        await Task.CompletedTask; // 非同期メソッドの形を保持

        var actionName = actionType.ToString();
        
        // 禁止アクションのチェック
        if (_constraints.ForbiddenActions.Contains(actionName))
        {
            _logger.LogWarning("Forbidden action attempted: {ActionType}", actionType);
            return ConstraintValidationResult.Failure(
                new ConstraintViolation
                {
                    Type = ConstraintType.ForbiddenAction,
                    ConstraintName = "ForbiddenActions",
                    Description = $"アクション '{actionName}' は禁止されています（設計変更を伴う可能性があるため）",
                    ActualValue = actionName,
                    ExpectedValue = string.Join(", ", _constraints.AllowedActions),
                    Severity = ViolationSeverity.Critical
                }
            );
        }

        // 許可アクションリストのチェック（ホワイトリスト方式）
        if (_constraints.AllowedActions.Any() && !_constraints.AllowedActions.Contains(actionName))
        {
            _logger.LogWarning("Action not in allowed list: {ActionType}", actionType);
            return ConstraintValidationResult.Failure(
                new ConstraintViolation
                {
                    Type = ConstraintType.ForbiddenAction,
                    ConstraintName = "AllowedActions",
                    Description = $"アクション '{actionName}' は許可リストに含まれていません",
                    ActualValue = actionName,
                    ExpectedValue = string.Join(", ", _constraints.AllowedActions),
                    Severity = ViolationSeverity.Error
                }
            );
        }

        _logger.LogDebug("Action validation passed: {ActionType}", actionType);
        return ConstraintValidationResult.Success();
    }

    public async Task<ConstraintValidationResult> ValidateSqlCodeAsync(string sqlCode)
    {
        await Task.CompletedTask; // 非同期メソッドの形を保持

        var violations = new List<ConstraintViolation>();

        // 禁止SQLパターンのチェック
        foreach (var forbiddenPattern in _constraints.ForbiddenSqlPatterns)
        {
            try
            {
                if (Regex.IsMatch(sqlCode, forbiddenPattern, RegexOptions.IgnoreCase))
                {
                    violations.Add(new ConstraintViolation
                    {
                        Type = ConstraintType.ForbiddenSqlPattern,
                        ConstraintName = "ForbiddenSqlPatterns",
                        Description = $"禁止されたSQLパターンが検出されました: {forbiddenPattern}",
                        ActualValue = ExtractMatchingText(sqlCode, forbiddenPattern),
                        ExpectedValue = "許可されたSQL構文のみ",
                        Severity = ViolationSeverity.Critical
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating SQL pattern {Pattern}", forbiddenPattern);
            }
        }

        if (violations.Any())
        {
            _logger.LogWarning("SQL validation failed with {Count} violations", violations.Count);
            return new ConstraintValidationResult { IsValid = false, Violations = violations };
        }

        _logger.LogDebug("SQL validation passed");
        return ConstraintValidationResult.Success();
    }

    public async Task<ConstraintValidationResult> ValidateViewChangeAsync(string newViewDefinition, string? originalDefinition = null)
    {
        await Task.CompletedTask;

        var violations = new List<ConstraintViolation>();

        // ビュー定義長のチェック
        if (newViewDefinition.Length > _constraints.MaxViewDefinitionLength)
        {
            violations.Add(new ConstraintViolation
            {
                Type = ConstraintType.ViewDefinitionTooLarge,
                ConstraintName = "MaxViewDefinitionLength",
                Description = $"ビュー定義が最大長を超えています",
                ActualValue = newViewDefinition.Length.ToString(),
                ExpectedValue = _constraints.MaxViewDefinitionLength.ToString(),
                Severity = ViolationSeverity.Error
            });
        }

        // 禁止ビューパターンのチェック
        foreach (var forbiddenPattern in _constraints.ForbiddenViewPatterns)
        {
            try
            {
                if (Regex.IsMatch(newViewDefinition, forbiddenPattern, RegexOptions.IgnoreCase))
                {
                    violations.Add(new ConstraintViolation
                    {
                        Type = ConstraintType.ForbiddenViewPattern,
                        ConstraintName = "ForbiddenViewPatterns",
                        Description = $"禁止されたビューパターンが検出されました: {forbiddenPattern}",
                        ActualValue = ExtractMatchingText(newViewDefinition, forbiddenPattern),
                        ExpectedValue = "基本的なSELECT文とJOINのみ",
                        Severity = ViolationSeverity.Critical
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating view pattern {Pattern}", forbiddenPattern);
            }
        }

        if (violations.Any())
        {
            _logger.LogWarning("View validation failed with {Count} violations", violations.Count);
            return new ConstraintValidationResult { IsValid = false, Violations = violations };
        }

        return ConstraintValidationResult.Success();
    }

    public async Task<ConstraintValidationResult> ValidatePerformanceAsync(double improvementPercentage, long executionTimeMs)
    {
        await Task.CompletedTask;

        var violations = new List<ConstraintViolation>();

        // 最小改善率のチェック
        if (improvementPercentage < _constraints.MinimumImprovementPercentage)
        {
            violations.Add(new ConstraintViolation
            {
                Type = ConstraintType.InsufficientImprovement,
                ConstraintName = "MinimumImprovementPercentage",
                Description = "改善効果が不十分です",
                ActualValue = $"{improvementPercentage:F2}%",
                ExpectedValue = $"{_constraints.MinimumImprovementPercentage:F2}%以上",
                Severity = ViolationSeverity.Warning
            });
        }

        // 最大実行時間のチェック
        if (executionTimeMs > _constraints.MaxExecutionTimeMs)
        {
            violations.Add(new ConstraintViolation
            {
                Type = ConstraintType.ExcessiveExecutionTime,
                ConstraintName = "MaxExecutionTimeMs",
                Description = "実行時間が許容範囲を超えています",
                ActualValue = $"{executionTimeMs}ms",
                ExpectedValue = $"{_constraints.MaxExecutionTimeMs}ms以下",
                Severity = ViolationSeverity.Error
            });
        }

        if (violations.Any())
        {
            return new ConstraintValidationResult { IsValid = false, Violations = violations };
        }

        return ConstraintValidationResult.Success();
    }

    public async Task<ConstraintValidationResult> ValidateAllAsync(OptimizationActionType actionType, string? sqlCode = null, string? viewDefinition = null)
    {
        var allViolations = new List<ConstraintViolation>();
        var allWarnings = new List<string>();

        // アクション検証
        var actionResult = await ValidateActionAsync(actionType);
        if (!actionResult.IsValid)
        {
            allViolations.AddRange(actionResult.Violations);
        }
        allWarnings.AddRange(actionResult.Warnings);

        // SQLコード検証
        if (!string.IsNullOrEmpty(sqlCode))
        {
            var sqlResult = await ValidateSqlCodeAsync(sqlCode);
            if (!sqlResult.IsValid)
            {
                allViolations.AddRange(sqlResult.Violations);
            }
            allWarnings.AddRange(sqlResult.Warnings);
        }

        // ビュー定義検証
        if (!string.IsNullOrEmpty(viewDefinition))
        {
            var viewResult = await ValidateViewChangeAsync(viewDefinition);
            if (!viewResult.IsValid)
            {
                allViolations.AddRange(viewResult.Violations);
            }
            allWarnings.AddRange(viewResult.Warnings);
        }

        return new ConstraintValidationResult
        {
            IsValid = !allViolations.Any(),
            Violations = allViolations,
            Warnings = allWarnings
        };
    }

    /// <summary>
    /// 正規表現パターンにマッチした部分のテキストを抽出
    /// </summary>
    private static string ExtractMatchingText(string input, string pattern)
    {
        try
        {
            var match = Regex.Match(input, pattern, RegexOptions.IgnoreCase);
            return match.Success ? match.Value : "パターンマッチ";
        }
        catch
        {
            return "パターンマッチ";
        }
    }
}