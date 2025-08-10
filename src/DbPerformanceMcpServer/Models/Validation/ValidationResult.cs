namespace DbPerformanceMcpServer.Models.Validation;

/// <summary>
/// 結果セット検証結果
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// 検証が成功したか
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// ベースラインチェックサム
    /// </summary>
    public string BaselineChecksum { get; set; } = string.Empty;

    /// <summary>
    /// 現在のチェックサム
    /// </summary>
    public string CurrentChecksum { get; set; } = string.Empty;

    /// <summary>
    /// 検証実行時刻
    /// </summary>
    public DateTime ValidationTimestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 検証エラーメッセージ（失敗時）
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// データ差分レポート（失敗時）
    /// </summary>
    public DataDiffReport? DataDiffReport { get; set; }

    /// <summary>
    /// 検証にかかった時間（ミリ秒）
    /// </summary>
    public long ValidationDurationMs { get; set; }
}

/// <summary>
/// データ差分レポート
/// </summary>
public class DataDiffReport
{
    /// <summary>
    /// 行数の差分
    /// </summary>
    public long RowCountDifference { get; set; }

    /// <summary>
    /// ベースライン行数
    /// </summary>
    public long BaselineRowCount { get; set; }

    /// <summary>
    /// 現在の行数
    /// </summary>
    public long CurrentRowCount { get; set; }

    /// <summary>
    /// カラム数の差分
    /// </summary>
    public int ColumnCountDifference { get; set; }

    /// <summary>
    /// データ型の差分があるか
    /// </summary>
    public bool HasDataTypeDifferences { get; set; }

    /// <summary>
    /// NULL値の分布に差分があるか
    /// </summary>
    public bool HasNullDistributionDifferences { get; set; }

    /// <summary>
    /// 詳細差分メッセージ
    /// </summary>
    public List<string> DifferenceMessages { get; set; } = new();
}