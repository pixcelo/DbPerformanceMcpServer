using DbPerformanceMcpServer.Models.Optimization;

namespace DbPerformanceMcpServer.Services;

/// <summary>
/// 最適化制約の検証サービス
/// </summary>
public interface IConstraintValidator
{
    /// <summary>
    /// 最適化アクションが許可されているかをチェック
    /// </summary>
    /// <param name="actionType">最適化アクション種別</param>
    /// <returns>検証結果</returns>
    Task<ConstraintValidationResult> ValidateActionAsync(OptimizationActionType actionType);

    /// <summary>
    /// SQLコードが制約に違反していないかをチェック
    /// </summary>
    /// <param name="sqlCode">実行予定のSQLコード</param>
    /// <returns>検証結果</returns>
    Task<ConstraintValidationResult> ValidateSqlCodeAsync(string sqlCode);

    /// <summary>
    /// ビュー定義の変更が制約に違反していないかをチェック
    /// </summary>
    /// <param name="newViewDefinition">新しいビュー定義</param>
    /// <param name="originalDefinition">元のビュー定義</param>
    /// <returns>検証結果</returns>
    Task<ConstraintValidationResult> ValidateViewChangeAsync(string newViewDefinition, string? originalDefinition = null);

    /// <summary>
    /// パフォーマンス改善結果が制約を満たしているかをチェック
    /// </summary>
    /// <param name="improvementPercentage">改善率</param>
    /// <param name="executionTimeMs">実行時間</param>
    /// <returns>検証結果</returns>
    Task<ConstraintValidationResult> ValidatePerformanceAsync(double improvementPercentage, long executionTimeMs);

    /// <summary>
    /// 複数の制約チェックを一括実行
    /// </summary>
    /// <param name="actionType">アクション種別</param>
    /// <param name="sqlCode">SQLコード</param>
    /// <param name="viewDefinition">ビュー定義</param>
    /// <returns>統合検証結果</returns>
    Task<ConstraintValidationResult> ValidateAllAsync(OptimizationActionType actionType, string? sqlCode = null, string? viewDefinition = null);
}

/// <summary>
/// 制約検証結果
/// </summary>
public class ConstraintValidationResult
{
    /// <summary>
    /// 検証が成功したか
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// 違反した制約の詳細
    /// </summary>
    public List<ConstraintViolation> Violations { get; set; } = new();

    /// <summary>
    /// 警告メッセージ（実行は可能だが注意が必要）
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// 検証実行時刻
    /// </summary>
    public DateTime ValidatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 成功した検証結果を作成
    /// </summary>
    public static ConstraintValidationResult Success() => new() { IsValid = true };

    /// <summary>
    /// 失敗した検証結果を作成
    /// </summary>
    public static ConstraintValidationResult Failure(params ConstraintViolation[] violations) => new()
    {
        IsValid = false,
        Violations = violations.ToList()
    };
}

/// <summary>
/// 制約違反の詳細
/// </summary>
public class ConstraintViolation
{
    /// <summary>
    /// 違反の種別
    /// </summary>
    public ConstraintType Type { get; set; }

    /// <summary>
    /// 違反した制約の名前
    /// </summary>
    public string ConstraintName { get; set; } = string.Empty;

    /// <summary>
    /// 違反の説明
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 違反した値（実際の値）
    /// </summary>
    public string? ActualValue { get; set; }

    /// <summary>
    /// 期待される値（制約の基準値）
    /// </summary>
    public string? ExpectedValue { get; set; }

    /// <summary>
    /// 重要度レベル
    /// </summary>
    public ViolationSeverity Severity { get; set; } = ViolationSeverity.Error;
}

/// <summary>
/// 制約の種別
/// </summary>
public enum ConstraintType
{
    ForbiddenAction,
    ForbiddenSqlPattern,
    ForbiddenViewPattern,
    InsufficientImprovement,
    ExcessiveExecutionTime,
    ViewDefinitionTooLarge
}

/// <summary>
/// 違反の重要度
/// </summary>
public enum ViolationSeverity
{
    Warning,
    Error,
    Critical
}