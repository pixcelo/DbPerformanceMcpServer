using DbPerformanceMcpServer.Models.Validation;

namespace DbPerformanceMcpServer.Services;

/// <summary>
/// 結果セット検証サービス
/// </summary>
public interface IValidationService
{
    /// <summary>
    /// ビューの結果セットチェックサムを計算
    /// </summary>
    Task<string> CalculateResultChecksumAsync(string viewName, CancellationToken cancellationToken = default);

    /// <summary>
    /// 結果セットの同一性を検証
    /// </summary>
    Task<bool> ValidateResultIntegrityAsync(string baselineChecksum, string currentChecksum);

    /// <summary>
    /// 詳細な検証結果を生成
    /// </summary>
    Task<ValidationResult> ValidateWithDetailAsync(string viewName, string baselineChecksum, CancellationToken cancellationToken = default);

    /// <summary>
    /// データ差分レポートを生成
    /// </summary>
    Task<DataDiffReport?> GenerateDataDiffAsync(string viewName, string baselineSnapshotPath, CancellationToken cancellationToken = default);
}