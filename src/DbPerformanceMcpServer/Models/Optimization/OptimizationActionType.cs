namespace DbPerformanceMcpServer.Models.Optimization;

/// <summary>
/// 最適化アクション種別
/// </summary>
public enum OptimizationActionType
{
    /// <summary>
    /// 統計情報の更新
    /// </summary>
    UpdateStatistics,

    /// <summary>
    /// 不要なDISTINCTの削除
    /// </summary>
    RemoveUnnecessaryDistinct,

    /// <summary>
    /// サブクエリをJOINに変換
    /// </summary>
    ConvertSubqueryToJoin,

    /// <summary>
    /// 暗黙の型変換を修正
    /// </summary>
    FixImplicitConversion,

    /// <summary>
    /// 文字列連結の最適化（SQL Server 2016互換）
    /// </summary>
    OptimizeStringConcatenation,

    /// <summary>
    /// 計算列の事前計算
    /// </summary>
    PrecomputeCalculatedColumns,

    /// <summary>
    /// 不要なSort操作の削除
    /// </summary>
    RemoveUnnecessarySort,

    /// <summary>
    /// EXISTS をJOINに変換
    /// </summary>
    ConvertExistsToJoin,

    /// <summary>
    /// IN をJOINに変換
    /// </summary>
    ConvertInToJoin,

    /// <summary>
    /// 文字列操作の最適化（LTRIM/RTRIM使用）
    /// </summary>
    OptimizeStringOperations,

    /// <summary>
    /// テーブルスキャンの最適化
    /// </summary>
    OptimizeTableScans
}