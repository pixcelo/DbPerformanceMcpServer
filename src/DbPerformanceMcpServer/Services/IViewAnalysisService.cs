using DbPerformanceMcpServer.Models.Analysis;

namespace DbPerformanceMcpServer.Services;

/// <summary>
/// ビュー分析サービス
/// </summary>
public interface IViewAnalysisService
{
    /// <summary>
    /// ビューのベースライン分析を実行（フェーズ1）
    /// </summary>
    Task<ViewAnalysisResult> AnalyzeBaselineAsync(string viewIdentifier, string? snapshotBasePath = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// ビューの定義を取得
    /// </summary>
    Task<string> GetViewDefinitionAsync(string viewName, CancellationToken cancellationToken = default);

    /// <summary>
    /// ビューの結果セットチェックサムを計算
    /// </summary>
    Task<string> CalculateResultChecksumAsync(string viewName, CancellationToken cancellationToken = default);

    /// <summary>
    /// パフォーマンスメトリクスを測定
    /// </summary>
    Task<PerformanceMetrics> MeasurePerformanceAsync(string viewName, int runs = 3, CancellationToken cancellationToken = default);

    /// <summary>
    /// 実行プランを取得・分析
    /// </summary>
    Task<ExecutionPlanAnalysis> AnalyzeExecutionPlanAsync(string viewName, CancellationToken cancellationToken = default);

    /// <summary>
    /// ビュー識別子がファイルパスかビュー名かを判定
    /// </summary>
    bool IsFilePath(string viewIdentifier);

    /// <summary>
    /// ファイルからビュー定義を読み取り
    /// </summary>
    Task<string> ReadViewDefinitionFromFileAsync(string filePath, CancellationToken cancellationToken = default);
}