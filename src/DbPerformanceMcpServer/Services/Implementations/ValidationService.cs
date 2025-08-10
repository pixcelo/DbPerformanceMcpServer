using DbPerformanceMcpServer.Models.Validation;

namespace DbPerformanceMcpServer.Services;

/// <summary>
/// 結果セット検証サービス実装
/// </summary>
public class ValidationService : IValidationService
{
    private readonly ISqlConnectionService _sqlConnection;

    public ValidationService(ISqlConnectionService sqlConnection)
    {
        _sqlConnection = sqlConnection;
    }

    public Task<string> CalculateResultChecksumAsync(string viewName, CancellationToken cancellationToken = default)
    {
        // TODO: 実装
        throw new NotImplementedException();
    }

    public Task<bool> ValidateResultIntegrityAsync(string baselineChecksum, string currentChecksum)
    {
        return Task.FromResult(string.Equals(baselineChecksum, currentChecksum, StringComparison.Ordinal));
    }

    public Task<ValidationResult> ValidateWithDetailAsync(string viewName, string baselineChecksum, CancellationToken cancellationToken = default)
    {
        // TODO: 実装
        throw new NotImplementedException();
    }

    public Task<DataDiffReport?> GenerateDataDiffAsync(string viewName, string baselineSnapshotPath, CancellationToken cancellationToken = default)
    {
        // TODO: 実装
        throw new NotImplementedException();
    }
}