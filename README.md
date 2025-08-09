# DbPerformanceMcpServer

SQL Server ビューパフォーマンス最適化用MCPサーバー

## 概要

このMCPサーバーは、SQL Serverのビューパフォーマンスを段階的に改善するためのツールです。
データセットと並び順を完全に維持しながら、実行速度を最適化します。

## 主な機能

### フェーズ1: ベースライン分析
- ビュー定義の取得
- 実行プラン分析
- パフォーマンスメトリクス測定
- 結果セットチェックサム計算
- ボトルネック特定と改善提案

### フェーズ2: 段階的改善
- 1つずつ改善アクションを実行
- 結果セット同一性の厳密検証
- パフォーマンス測定
- 自動ロールバック機能

### フェーズ3: レポート生成
- 改善前後の詳細比較
- 成功・失敗した改善の履歴
- 推奨事項の提示

## サポートする改善アクション

- 統計情報の更新
- 不要なDISTINCTの削除
- サブクエリをJOINに変換
- 暗黙の型変換修正
- 文字列操作の最適化（SQL Server 2016互換）

## 使用方法

### MCPツール一覧

1. **analyze-view-baseline**: ベースライン分析実行
2. **execute-optimization-step**: 単一改善ステップ実行
3. **optimize-view-fully**: 完全自動最適化実行
4. **generate-final-report**: 最終レポート生成
5. **measure-view-performance**: パフォーマンス測定
6. **validate-view-results**: データ整合性確認

### 設定

`appsettings.json`で以下を設定：

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=PMS;Integrated Security=true;TrustServerCertificate=true;"
  },
  "DbPerformanceOptimizer": {
    "SnapshotBasePath": "./performance_snapshots/",
    "MaxOptimizationSteps": 10,
    "ValidationTimeoutSeconds": 300,
    "PerformanceMeasurementRuns": 3,
    "SQL2016Compatible": true
  }
}
```

## 安全性

- **結果同一性保証**: SHA2_256ハッシュでデータ整合性を厳密チェック
- **自動ロールバック**: 検証失敗時は自動的に元の状態に復元
- **SQL Server 2016互換**: 安定した機能のみ使用

## 制約事項

- インデックス追加・CTE新規追加は禁止
- 複数改善の同時実装は禁止
- SQL Server 2017以降の機能は使用禁止