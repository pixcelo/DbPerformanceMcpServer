# DbPerformanceMcpServer プロジェクト概要

## 📋 プロジェクト情報

**作成日**: 2025年8月9日  
**開発者**: TetsuroKawakami  
**目的**: SQL Server ビューパフォーマンス最適化用MCPサーバー  
**基盤**: 既存RoslynMcpServer構造を参考

## 🎯 プロジェクト目標

### 主要目的
- **データセット完全保持** - 結果セットと並び順を1行たりとも変更しない
- **段階的改善** - 1つずつ改善アクションを実行・検証
- **自動ロールバック** - 検証失敗時は元の状態に自動復元
- **詳細レポート** - 改善前後の詳細比較とファイル出力

### 解決する課題
- プロンプトベースの指示では人為的ミスが発生
- ハッシュ値検証の省略リスク
- 複数改善の同時実装による問題特定困難
- 改善プロセスの標準化不足

## 🏗️ アーキテクチャ設計

### システム構成
```
DbPerformanceMcpServer/
├── src/DbPerformanceMcpServer/
│   ├── Configuration/          # 設定クラス
│   ├── Models/                 # データモデル
│   │   ├── Analysis/           # 分析結果モデル
│   │   ├── Optimization/       # 最適化モデル  
│   │   └── Validation/         # 検証モデル
│   ├── Services/               # サービス層
│   │   ├── Implementations/    # 具象実装
│   │   └── I*.cs              # インターフェース
│   ├── Tools/                  # MCPツール
│   └── Utils/                  # ユーティリティ
├── docs/                       # プロジェクトドキュメント
└── tests/                      # テストプロジェクト
```

### 主要サービス
1. **ISqlConnectionService** - SQL Server接続・クエリ実行
2. **IViewAnalysisService** - ベースライン分析（フェーズ1）
3. **IOptimizationService** - 改善実行・検証（フェーズ2）
4. **IValidationService** - 結果セット検証
5. **ISnapshotService** - ファイル出力管理
6. **IExecutionPlanAnalyzer** - 実行プラン分析
7. **IOptimizationOrchestrator** - 全体プロセス制御

## 🔧 MCPツール一覧

| ツール名 | 機能 | フェーズ |
|---------|------|---------|
| `analyze-view-baseline` | ベースライン分析実行 | 1 |
| `execute-optimization-step` | 単一改善ステップ実行 | 2 |
| `optimize-view-fully` | 完全自動最適化実行 | 1-3 |
| `generate-final-report` | 最終レポート生成 | 3 |
| `measure-view-performance` | パフォーマンス測定 | - |
| `validate-view-results` | データ整合性確認 | - |

## ⚙️ 技術スタック

### 基盤技術
- **.NET 8.0** - 実行環境
- **Microsoft.Data.SqlClient 5.2.2** - SQL Server接続
- **ModelContextProtocol 0.3.0** - MCPサーバー機能
- **System.Text.Json 10.0.0** - JSON処理

### 配布方式
- **単一実行ファイル** - DbPerformanceMcpServer.exe
- **自己完結型** - .NET Runtimeバンドル
- **プロセス独立** - 複数インスタンス対応

## 📊 対応する改善アクション

### SQL Server 2016互換
1. **UpdateStatistics** - 統計情報更新（WITH FULLSCAN）
2. **RemoveUnnecessaryDistinct** - 不要なDISTINCT削除
3. **ConvertSubqueryToJoin** - サブクエリ→結合変換
4. **FixImplicitConversion** - 暗黙の型変換修正
5. **OptimizeStringConcatenation** - 文字列連結最適化
6. **PrecomputeCalculatedColumns** - 計算列事前計算
7. **RemoveUnnecessarySort** - 不要Sort削除
8. **OptimizeStringOperations** - LTRIM/RTRIM使用

### 実行プラン分析項目
- **高コスト操作** - 30%以上のコスト操作
- **カーディナリティ推定エラー** - 10倍以上の乖離
- **暗黙の型変換** - CONVERT_IMPLICIT検出
- **Table Scan/Index Scan** - 非効率スキャン検出

## 🔒 安全性設計

### データ完全性保証
- **SHA2_256ハッシュ** - 結果セット同一性の厳密チェック
- **自動ロールバック** - 検証失敗時の自動復元
- **段階的実行** - 1アクションずつの慎重な実行

### 制約事項（安全性確保）
- ❌ インデックス追加・CTE新規追加禁止
- ❌ 複数改善の同時実装禁止
- ❌ SQL Server 2017以降機能使用禁止
- ❌ ハッシュ値検証の省略禁止

## 🎪 使用例

### 基本的な使用フロー
```bash
# 1. ベースライン分析
claude mcp call DbPerformanceMcp analyze-view-baseline "dbo.V_SlowSalesReport"

# 2. 段階的改善
claude mcp call DbPerformanceMcp execute-optimization-step "dbo.V_SlowSalesReport" "UpdateStatistics" "dbo.Orders"

# 3. 最終レポート生成  
claude mcp call DbPerformanceMcp generate-final-report "dbo.V_SlowSalesReport" "./performance_snapshots/"
```

### 出力ファイル構造
```
performance_snapshots/dbo.V_SlowSalesReport/
├── 00_Baseline/
│   ├── view_definition.sql
│   ├── result_checksum.txt
│   ├── performance_metrics.json
│   └── execution_plan.xml
├── 01_UpdateStatistics_Orders/
│   ├── action.sql
│   ├── result_checksum.txt
│   ├── performance_metrics.json
│   └── status.json
└── final_report.md
```

## 📈 プロジェクト進捗

- ✅ **アーキテクチャ設計** - 完了
- ✅ **プロジェクト構造** - 完了
- ✅ **モデルクラス** - 完了
- ✅ **サービスインターフェース** - 完了
- ✅ **MCPツール定義** - 完了
- ✅ **単一実行ファイル化** - 完了
- ✅ **骨格実装** - 完了
- ⏳ **MCP接続確立** - 作業中
- ⏳ **SQL Server実装** - 未着手
- ⏳ **実行プラン分析** - 未着手
- ⏳ **最適化ロジック** - 未着手

## 🔗 参考資料

- [元プロンプト指示書](../../pms-docs/docs/playbooks/implementation/db-performance.md)
- [RoslynMcpServer参考実装](../RoslynMcpServer/)
- [アーキテクチャ詳細設計](./architecture-detail.md)
- [開発進捗ログ](./development-progress.md)