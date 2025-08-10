using DbPerformanceMcpServer.Models.Analysis;
using DbPerformanceMcpServer.Models.Optimization;

namespace DbPerformanceMcpServer.Services;

/// <summary>
/// スナップショット管理サービス
/// </summary>
public interface ISnapshotService
{
    /// <summary>
    /// ベースラインスナップショットを保存
    /// </summary>
    Task SaveBaselineSnapshotAsync(
        string viewName, 
        ViewAnalysisResult analysisResult, 
        string snapshotBasePath, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 最適化ステップのスナップショットを保存
    /// </summary>
    Task SaveOptimizationSnapshotAsync(
        string viewName, 
        OptimizationSnapshot snapshot, 
        string snapshotBasePath, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// スナップショットディレクトリを作成
    /// </summary>
    Task<string> CreateSnapshotDirectoryAsync(string viewName, string snapshotBasePath, string snapshotId = "00_Baseline");

    /// <summary>
    /// 保存されたスナップショットの一覧を取得
    /// </summary>
    Task<List<string>> GetSnapshotListAsync(string viewName, string snapshotBasePath);

    /// <summary>
    /// 指定されたスナップショットを読み込み
    /// </summary>
    Task<OptimizationSnapshot?> LoadOptimizationSnapshotAsync(string viewName, string snapshotId, string snapshotBasePath);

    /// <summary>
    /// ベースラインスナップショットを読み込み
    /// </summary>
    Task<ViewAnalysisResult?> LoadBaselineSnapshotAsync(string viewName, string snapshotBasePath);
}