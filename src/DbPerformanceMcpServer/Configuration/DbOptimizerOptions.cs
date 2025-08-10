namespace DbPerformanceMcpServer.Configuration;

/// <summary>
/// DBパフォーマンス最適化の設定オプション
/// </summary>
public class DbOptimizerOptions
{
    /// <summary>
    /// スナップショット保存ベースパス
    /// </summary>
    public string SnapshotBasePath { get; set; } = "./performance_snapshots/";

    /// <summary>
    /// 最大最適化ステップ数
    /// </summary>
    public int MaxOptimizationSteps { get; set; } = 10;

    /// <summary>
    /// 検証タイムアウト（秒）
    /// </summary>
    public int ValidationTimeoutSeconds { get; set; } = 300;

    /// <summary>
    /// パフォーマンス測定実行回数
    /// </summary>
    public int PerformanceMeasurementRuns { get; set; } = 3;

    /// <summary>
    /// SQL Server 2016互換モード
    /// </summary>
    public bool SQL2016Compatible { get; set; } = true;

    /// <summary>
    /// 詳細ログ有効化
    /// </summary>
    public bool EnableDetailedLogging { get; set; } = true;

    /// <summary>
    /// 測定間でバッファをクリアするか
    /// </summary>
    public bool BufferClearBetweenRuns { get; set; } = false;

    /// <summary>
    /// 最適化制約設定
    /// </summary>
    public OptimizationConstraints Constraints { get; set; } = new();
}

/// <summary>
/// 最適化実行時の制約設定
/// </summary>
public class OptimizationConstraints
{
    /// <summary>
    /// 禁止する最適化アクション（設計変更を伴うもの）
    /// </summary>
    public List<string> ForbiddenActions { get; set; } = new()
    {
        "CreateIndex",
        "DropIndex", 
        "CreateCTE",
        "AddPartitioning",
        "CreateView",
        "AlterTableStructure"
    };

    /// <summary>
    /// 許可する最適化アクションのみ（ホワイトリスト）
    /// </summary>
    public List<string> AllowedActions { get; set; } = new()
    {
        "UpdateStatistics",
        "RemoveUnnecessaryDistinct",
        "ConvertSubqueryToJoin",
        "FixImplicitConversion",
        "OptimizeStringConcatenation",
        "PrecomputeCalculatedColumns",
        "RemoveUnnecessarySort",
        "OptimizeStringOperations"
    };

    /// <summary>
    /// 禁止するSQL構文パターン
    /// </summary>
    public List<string> ForbiddenSqlPatterns { get; set; } = new()
    {
        "CREATE INDEX",
        "DROP INDEX",
        "WITH.*AS.*\\(",  // CTE構文
        "PARTITION BY",
        "ALTER TABLE.*ADD",
        "ALTER TABLE.*DROP"
    };

    /// <summary>
    /// ビュー定義変更時の禁止パターン
    /// </summary>
    public List<string> ForbiddenViewPatterns { get; set; } = new()
    {
        "WITH.*AS.*\\(",  // CTE追加
        "CREATE.*VIEW",   // 新規ビュー作成
        "UNION ALL",      // 大幅な構造変更
        "CROSS APPLY",    // 複雑な結合
        "OUTER APPLY"
    };

    /// <summary>
    /// 最小改善率（これ以下の改善は無効とみなす）
    /// </summary>
    public double MinimumImprovementPercentage { get; set; } = 5.0;

    /// <summary>
    /// 最大実行時間（ミリ秒）これを超える改善は危険とみなす
    /// </summary>
    public long MaxExecutionTimeMs { get; set; } = 30000;

    /// <summary>
    /// ビュー定義の最大長（この長さを超える変更は禁止）
    /// </summary>
    public int MaxViewDefinitionLength { get; set; } = 50000;
}