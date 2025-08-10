using DbPerformanceMcpServer.Models.Analysis;
using DbPerformanceMcpServer.Models.Optimization;

namespace DbPerformanceMcpServer.Services;

/// <summary>
/// スナップショット管理サービス実装
/// </summary>
public class SnapshotService : ISnapshotService
{
    public Task SaveBaselineSnapshotAsync(string viewName, ViewAnalysisResult analysisResult, string snapshotBasePath, CancellationToken cancellationToken = default)
    {
        // TODO: 実装
        throw new NotImplementedException();
    }

    public Task SaveOptimizationSnapshotAsync(string viewName, OptimizationSnapshot snapshot, string snapshotBasePath, CancellationToken cancellationToken = default)
    {
        // TODO: 実装
        throw new NotImplementedException();
    }

    public Task<string> CreateSnapshotDirectoryAsync(string viewName, string snapshotBasePath, string snapshotId = "00_Baseline")
    {
        // TODO: 実装
        throw new NotImplementedException();
    }

    public Task<List<string>> GetSnapshotListAsync(string viewName, string snapshotBasePath)
    {
        // TODO: 実装
        throw new NotImplementedException();
    }

    public Task<OptimizationSnapshot?> LoadOptimizationSnapshotAsync(string viewName, string snapshotId, string snapshotBasePath)
    {
        // TODO: 実装
        throw new NotImplementedException();
    }

    public Task<ViewAnalysisResult?> LoadBaselineSnapshotAsync(string viewName, string snapshotBasePath)
    {
        // TODO: 実装
        throw new NotImplementedException();
    }
}