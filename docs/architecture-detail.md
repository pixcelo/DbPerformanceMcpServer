# DbPerformanceMcpServer アーキテクチャ詳細設計

## 🏗️ システム全体構成

### レイヤー構造
```
┌─────────────────────────────────────┐
│         MCP Client (Claude)         │ ← MCPプロトコル通信
├─────────────────────────────────────┤
│         DbPerformanceMcpTools       │ ← MCPツールエンドポイント
├─────────────────────────────────────┤
│        Service Layer (Business)     │ ← ビジネスロジック
├─────────────────────────────────────┤
│       Infrastructure Layer          │ ← SQL Server・ファイルIO
├─────────────────────────────────────┤
│    Configuration & DI Container     │ ← 設定・依存注入
└─────────────────────────────────────┘
```

## 📋 詳細クラス設計

### 1. MCPツール層
```csharp
public class DbPerformanceMcpTools
{
    // 6つの主要エンドポイント
    public async Task<string> AnalyzeViewBaseline(string viewIdentifier, string? snapshotBasePath)
    public async Task<string> ExecuteOptimizationStep(string viewName, OptimizationActionType actionType, string? targetObject, string? snapshotBasePath)
    public async Task<string> OptimizeViewFully(string viewIdentifier, int? maxSteps, string? snapshotBasePath)
    public async Task<string> GenerateFinalReport(string viewName, string snapshotBasePath)
    public async Task<string> MeasureViewPerformance(string viewName, int? measurementRuns)
    public async Task<string> ValidateViewResults(string viewName, string? baselineChecksum)
}
```

### 2. サービス層の責務分離

#### 🔍 IViewAnalysisService - 分析専門
```csharp
public interface IViewAnalysisService
{
    // フェーズ1: ベースライン分析の完全自動化
    Task<ViewAnalysisResult> AnalyzeBaselineAsync(string viewIdentifier, string? snapshotBasePath = null);
    
    // 個別分析機能
    Task<string> GetViewDefinitionAsync(string viewName);
    Task<string> CalculateResultChecksumAsync(string viewName);
    Task<PerformanceMetrics> MeasurePerformanceAsync(string viewName, int runs = 3);
    Task<ExecutionPlanAnalysis> AnalyzeExecutionPlanAsync(string viewName);
    
    // 入力判定・ファイル処理
    bool IsFilePath(string viewIdentifier);
    Task<string> ReadViewDefinitionFromFileAsync(string filePath);
}
```

#### ⚙️ IOptimizationService - 改善実行専門
```csharp
public interface IOptimizationService
{
    // フェーズ2: 段階的改善の中心機能
    Task<OptimizationSnapshot> ExecuteOptimizationStepAsync(string viewName, OptimizationActionType actionType, string? targetObject = null, string? snapshotBasePath = null);
    
    // 改善関連機能
    Task<string> GenerateOptimizationSqlAsync(OptimizationActionType actionType, string viewName, string? targetObject = null);
    Task RollbackOptimizationStepAsync(string viewName, OptimizationActionType actionType, string? originalDefinition = null);
    
    // 検証・測定
    Task<ValidationResult> ValidateResultIntegrityAsync(string viewName, string baselineChecksum);
    Task<PerformanceMetrics> MeasurePerformanceAsync(string viewName, int? runs = null);
    Task<string> CalculateResultChecksumAsync(string viewName);
    
    // フェーズ3: レポート生成
    Task<string> GenerateFinalReportAsync(string viewName, string snapshotBasePath);
}
```

#### 🔍 IValidationService - 検証専門
```csharp
public interface IValidationService
{
    // 結果セット同一性の厳密検証（最重要機能）
    Task<string> CalculateResultChecksumAsync(string viewName);
    Task<bool> ValidateResultIntegrityAsync(string baselineChecksum, string currentChecksum);
    Task<ValidationResult> ValidateWithDetailAsync(string viewName, string baselineChecksum);
    
    // 差分分析（デバッグ用）
    Task<DataDiffReport?> GenerateDataDiffAsync(string viewName, string baselineSnapshotPath);
}
```

#### 🎯 IExecutionPlanAnalyzer - 実行プラン解析専門
```csharp
public interface IExecutionPlanAnalyzer
{
    // 実行プランXMLの完全解析
    Task<ExecutionPlanAnalysis> AnalyzeExecutionPlanAsync(string executionPlanXml);
    
    // 特定問題の抽出
    Task<List<HighCostOperation>> ExtractHighCostOperationsAsync(string executionPlanXml);
    Task<List<CardinalityEstimationError>> DetectCardinalityErrorsAsync(string executionPlanXml);
    Task<List<ImplicitConversion>> DetectImplicitConversionsAsync(string executionPlanXml);
    
    // 改善提案生成
    Task<List<OptimizationSuggestion>> GenerateOptimizationSuggestionsAsync(ExecutionPlanAnalysis planAnalysis);
    
    // 改善前後比較
    Task<ExecutionPlanComparison> CompareExecutionPlansAsync(string beforePlanXml, string afterPlanXml);
}
```

#### 🎪 IOptimizationOrchestrator - 全体制御専門
```csharp
public interface IOptimizationOrchestrator
{
    // フェーズ1-3の完全自動実行
    Task<OptimizationSession> OptimizeViewFullyAsync(string viewIdentifier, int? maxSteps = null, string? snapshotBasePath = null);
    
    // セッション管理
    Task<OptimizationSession> StartOptimizationSessionAsync(string viewIdentifier, string? snapshotBasePath = null);
    Task<OptimizationSnapshot> ExecuteNextOptimizationStepAsync(OptimizationSession session);
    Task<string> FinalizeOptimizationSessionAsync(OptimizationSession session);
    Task<OptimizationSessionStatus> GetSessionStatusAsync(OptimizationSession session);
}
```

## 🗃️ データモデル設計

### コア分析モデル
```csharp
// ViewAnalysisResult - フェーズ1の完全な分析結果
public class ViewAnalysisResult
{
    public string ViewName { get; set; }                    // 対象ビュー名
    public string ViewDefinition { get; set; }              // ビュー定義SQL
    public string ResultChecksum { get; set; }              // 結果セットハッシュ値
    public PerformanceMetrics BaselineMetrics { get; set; } // ベースライン性能
    public ExecutionPlanAnalysis ExecutionPlanAnalysis { get; set; } // 実行プラン解析
    public List<OptimizationSuggestion> Suggestions { get; set; }    // 改善提案リスト
    public DateTime AnalysisTimestamp { get; set; }         // 分析日時
}

// PerformanceMetrics - 詳細パフォーマンス指標
public class PerformanceMetrics
{
    public long ExecutionTimeMs { get; set; }        // 実行時間（ミリ秒）
    public long CpuTimeMs { get; set; }              // CPU時間
    public long LogicalReads { get; set; }           // 論理読み取り数
    public long PhysicalReads { get; set; }          // 物理読み取り数
    public long ReadAheadReads { get; set; }         // 先読み読み取り数
    public int ScanCount { get; set; }               // スキャン回数
    public int MeasurementRuns { get; set; }         // 測定回数
    public double? ExecutionTimeStdDev { get; set; } // 実行時間標準偏差
    public double? ImprovementPercentage { get; set; } // 改善率
    public DateTime Timestamp { get; set; }          // 測定日時
}
```

### 最適化実行モデル
```csharp
// OptimizationSnapshot - 各改善ステップの完全記録
public class OptimizationSnapshot
{
    public string SnapshotId { get; set; }           // "01", "02"...
    public string ActionName { get; set; }           // アクション名
    public string ActionSql { get; set; }            // 実行SQL文
    public string? ViewDefinitionBefore { get; set; } // 変更前定義
    public string? ViewDefinitionAfter { get; set; }  // 変更後定義
    public ValidationResult ValidationResult { get; set; } // 検証結果
    public PerformanceMetrics? PerformanceMetrics { get; set; } // 性能結果
    public OptimizationStatus Status { get; set; }   // 実行ステータス
    public string? ErrorMessage { get; set; }        // エラーメッセージ
    public DateTime StartTime { get; set; }          // 開始時刻
    public DateTime EndTime { get; set; }            // 終了時刻
    public double? ImprovementPercentage { get; set; } // 改善率
    public long BaselineExecutionTimeMs { get; set; } // ベースライン時間
    public long? ImprovedExecutionTimeMs { get; set; } // 改善後時間
}

// OptimizationSession - 全体セッション状態管理
public class OptimizationSession
{
    public string SessionId { get; set; }            // セッションID
    public string ViewName { get; set; }             // 対象ビュー名
    public string SnapshotBasePath { get; set; }     // 出力ベースパス
    public ViewAnalysisResult? BaselineAnalysis { get; set; } // ベースライン
    public List<OptimizationSnapshot> ExecutedSteps { get; set; } // 実行履歴
    public Queue<OptimizationSuggestion> PendingSuggestions { get; set; } // 待機中提案
    public DateTime StartTime { get; set; }          // セッション開始
    public DateTime? EndTime { get; set; }           // セッション終了
    public OptimizationSessionState State { get; set; } // セッション状態
    public int MaxSteps { get; set; }                // 最大ステップ数
    public int CurrentStepNumber => ExecutedSteps.Count + 1;
    public bool HasMoreOptimizations => PendingSuggestions.Count > 0 && CurrentStepNumber <= MaxSteps;
}
```

## 🔧 SQL実行設計

### 3段階の実行プラン取得精度
```csharp
public async Task<ExecutionPlanResult> GetExecutionPlanAsync(string viewName)
{
    // レベル1: 静的プラン（改善前分析用）
    var staticPlan = await GetStaticExecutionPlanAsync(viewName);
    
    // レベル2: 実行時プラン（改善後検証用）  
    var actualPlan = await GetActualExecutionPlanAsync(viewName);
    
    // レベル3: キャッシュプラン（運用時分析用）
    var cachedPlan = await GetCachedExecutionPlanAsync(viewName);
    
    return new ExecutionPlanResult(staticPlan, actualPlan, cachedPlan);
}
```

### 結果セット同一性検証（最重要）
```csharp
// SHA2_256による厳密ハッシュ値計算
private async Task<string> CalculateViewChecksumAsync(string viewName)
{
    var sql = $@"
        DECLARE @CheckSum NVARCHAR(MAX);
        SELECT @CheckSum = CONVERT(NVARCHAR(MAX), 
            HASHBYTES('SHA2_256', 
                (SELECT * FROM {viewName} ORDER BY (SELECT NULL) FOR XML RAW, BINARY BASE64)
            ), 2);
        SELECT @CheckSum AS ResultChecksum;";
    
    return await _sqlConnection.ExecuteScalarAsync<string>(sql);
}
```

## 📁 ファイル出力設計

### スナップショット構造
```
performance_snapshots/{viewName}/
├── 00_Baseline/                    # ベースライン（フェーズ1）
│   ├── view_definition.sql         # ビュー定義
│   ├── result_checksum.txt         # 結果ハッシュ値
│   ├── performance_metrics.json    # パフォーマンス指標
│   ├── execution_plan.xml          # 実行プラン
│   └── analysis_result.json        # 分析結果
├── 01_UpdateStatistics_Orders/     # 改善ステップ1
│   ├── action.sql                  # 実行SQL
│   ├── action_log.json             # 実行ログ
│   ├── result_checksum.txt         # 検証ハッシュ
│   ├── performance_metrics.json    # 改善後性能
│   ├── execution_plan.xml          # 改善後プラン
│   └── status.json                 # ステータス
├── 02_RemoveDistinct_ProductQuery/ # 改善ステップ2
│   ├── view_definition_before.sql  # 変更前定義
│   ├── view_definition_after.sql   # 変更後定義
│   ├── action.sql                  # ALTER VIEW文
│   └── ...                         # 上記と同様
└── final_report.md                 # 最終レポート（フェーズ3）
```

## 🔒 安全性アーキテクチャ

### 1. データ完全性保証
- **トランザクション管理** - 各改善ステップでトランザクション制御
- **ハッシュ値検証** - 結果セットの1行レベルでの同一性確認
- **自動ロールバック** - 検証失敗時の即座復元

### 2. エラーハンドリング戦略
```csharp
public async Task<OptimizationSnapshot> ExecuteOptimizationStepAsync(...)
{
    var checkpoint = await CreateCheckpointAsync();
    
    try
    {
        using var transaction = await BeginTransactionAsync();
        
        // 1. アクション実行
        await ExecuteActionAsync(actionType, targetObject);
        
        // 2. 結果検証（最重要）
        var validation = await ValidateResultIntegrityAsync(viewName, checkpoint.BaselineHash);
        if (!validation.IsValid)
        {
            await RollbackAsync(checkpoint);
            return CreateFailedSnapshot("結果セット不一致", validation);
        }
        
        // 3. パフォーマンス測定
        var performance = await MeasurePerformanceAsync(viewName);
        
        // 4. 改善判定
        if (performance.ImprovementPercentage < 5.0)
        {
            await RollbackAsync(checkpoint);
            return CreateFailedSnapshot("改善効果不足", performance);
        }
        
        await CommitTransactionAsync(transaction);
        return CreateSuccessSnapshot(validation, performance);
    }
    catch (Exception ex)
    {
        await RollbackAsync(checkpoint);
        return CreateFailedSnapshot($"実行エラー: {ex.Message}");
    }
}
```

## 🚀 パフォーマンス設計

### 最適化実行戦略
- **段階的実行** - 1アクションずつで問題箇所の特定容易化
- **優先度順実行** - 高インパクト改善から優先実行
- **早期終了** - 十分な改善達成時の自動停止

### キャッシュ戦略
- **実行プランキャッシュ** - 同一プランの再利用
- **統計情報キャッシュ** - テーブル統計の一時保存
- **結果セットキャッシュ** - 検証用データの一時保持

この設計により、**安全で確実なビューパフォーマンス改善**を実現します。