# CLAUDE.md

このファイルは、Claude Code (claude.ai/code) がこのリポジトリで作業する際のガイダンスを提供します。
必ず日本語で回答してください。

## プロジェクト概要

SQL Serverビューパフォーマンス**分析・提案専用**のModel Context Protocol (MCP)サーバーです。実際のデータベース変更は行わず、詳細な分析結果と最適化提案のみを生成します。最終的なALTER文の実行は人間が判断・実行します。

### 🔒 **重要な設計方針**
- **読み取り専用**: SELECT、実行プラン取得、統計測定のみ実行
- **提案生成**: 最適化SQLを生成するが実際には実行しない
- **人間判断**: 生成されたALTER文の実行は開発者・DBAが決定

## ビルドと開発コマンド

### ビルド
```bash
# Releaseモードでビルド（自己完結型実行ファイルを作成）
dotnet build --configuration Release

# 単一ファイル実行ファイルを公開（デフォルト対象: win-x64）
dotnet publish --configuration Release --runtime win-x64 --self-contained true

# 開発用のDebugモードビルド
dotnet build --configuration Debug
```

### 実行
```bash
# MCPサーバーを直接実行
dotnet run --project src/DbPerformanceMcpServer

# または公開された実行ファイルを実行
./DbPerformanceMcpServer.exe
```

### SQL Server接続設定
MCPツールを使用する前に、`appsettings.json`でSQL Server接続文字列を設定してください：

#### Windows認証の場合（推奨）
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=PerformanceTestDB;Integrated Security=true;TrustServerCertificate=true;"
  }
}
```

#### SQL Server認証の場合
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=PerformanceTestDB;User Id=sa;Password=YourPassword123;TrustServerCertificate=true;"
  }
}
```

#### リモートSQL Serverの場合
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=sql-server.company.com,1433;Database=ProductionDB;User Id=dboptimizer;Password=SecurePass123;Encrypt=true;TrustServerCertificate=false;"
  }
}
```

**接続文字列のパラメータ説明:**
- `Server`: SQL Serverインスタンス名（ポート指定可能）
- `Database`: 対象データベース名
- `Integrated Security=true`: Windows認証を使用
- `User Id/Password`: SQL Server認証の場合
- `TrustServerCertificate=true`: 開発環境用（本番では false を推奨）
- `Encrypt=true`: 通信の暗号化（本番環境推奨）

## アーキテクチャ

### サービス層設計
アプリケーションは分析・提案専用のクリーンなサービス指向アーキテクチャに従います：

- **IViewAnalysisService**: ベースライン分析とパフォーマンス測定（読み取り専用）
- **IOptimizationService**: 最適化提案SQL生成（実行はしない）
- **IOptimizationOrchestrator**: 分析セッション管理と包括的提案生成
- **IValidationService**: SHA2_256チェックサムを使用した結果検証（読み取り専用）
- **IExecutionPlanAnalyzer**: SQL実行プラン分析と最適化提案
- **ISqlConnectionService**: データベース接続と読み取り専用クエリ実行
- **ISnapshotService**: 分析結果とSQL提案のファイル出力管理

### 読み取り専用設計の安全性
- **DDL実行なし**: ALTER VIEW、UPDATE STATISTICS等は提案のみ生成
- **データ検証**: SHA2_256ハッシュによる結果セット同一性確認
- **提案品質保証**: 生成されるSQL文の構文・制約チェック
- **完全監査**: すべての分析過程と提案内容をファイル出力

## MCPツール（分析・提案専用）

ビュー分析と最適化提案のための6つのMCPツールを公開しています：

1. **analyze-view-baseline**: ベースライン分析の実行（実行プラン、パフォーマンス、チェックサム）
2. **generate-optimization-proposal**: 単一最適化提案の生成（SQL生成、実行はしない）
3. **analyze-and-propose-optimizations**: 包括的分析と最適化提案一覧生成
4. **generate-final-report**: 包括的な分析レポートと実行可能SQL文の生成
5. **measure-view-performance**: 現在のビューパフォーマンスの測定
6. **validate-view-results**: 結果チェックサムの計算/検証

### 🚨 **重要**: すべてのツールは分析専用
- DDL文（ALTER VIEW等）は**生成のみ**で実行しません
- 生成されたSQL文の実行は人間が判断・実行してください
- 本ツールは提案・検証・測定のみを行います

## 設定

`appsettings.json`の主要設定：

- `ConnectionStrings.DefaultConnection`: SQL Server接続文字列
- `DbPerformanceOptimizer.SnapshotBasePath`: パフォーマンススナップショットの出力ディレクトリ
- `DbPerformanceOptimizer.MaxOptimizationSteps`: セッションあたりの最大最適化ステップ数
- `DbPerformanceOptimizer.SQL2016Compatible`: SQL Server 2016+との互換性を確保
- `DbPerformanceOptimizer.Constraints`: 最適化制約設定（詳細は後述）

### 最適化制約システム
安全な最適化を実現するため、包括的な制約システムを実装：

- **ForbiddenActions**: インデックス作成、CTE追加など設計変更を伴うアクションを禁止
- **AllowedActions**: 統計更新、DISTINCT削除など安全なアクションのみを許可（ホワイトリスト）
- **ForbiddenSqlPatterns**: `CREATE INDEX`、`WITH...AS`など危険なSQL構文を正規表現で検出
- **ForbiddenViewPatterns**: ビュー定義でのCTE、CROSS APPLYなど複雑な構文を制限
- **パフォーマンス制約**: 最小改善率（5%）、最大実行時間（30秒）などの品質基準

詳細な制約設定については `docs/optimization-constraints.md` を参照してください。

## ファイル構造

```
src/DbPerformanceMcpServer/
├── Configuration/              # 設定オプションクラス
├── Models/
│   ├── Analysis/              # ベースライン分析と実行プランモデル
│   ├── Optimization/          # 最適化アクションとスナップショットモデル
│   └── Validation/            # 結果検証モデル
├── Services/
│   ├── Implementations/       # サービス実装
│   └── I*.cs                 # サービスインターフェース
├── Tools/
│   └── DbPerformanceMcpTools.cs  # MCPツールエンドポイント
└── Utils/                     # ユーティリティクラス
```

## サポートされる最適化アクション

- **UpdateStatistics**: FULLSCANでテーブル統計を更新
- **RemoveUnnecessaryDistinct**: 冗長なDISTINCT句を削除
- **ConvertSubqueryToJoin**: サブクエリをより効率的なJOINに変換
- **FixImplicitConversion**: 暗黙的データ型変換を修正
- **OptimizeStringConcatenation**: 文字列操作を最適化
- **PrecomputeCalculatedColumns**: 高コストな計算を事前計算
- **RemoveUnnecessarySort**: 冗長なORDER BY句を削除
- **OptimizeStringOperations**: 複雑な操作の代わりにLTRIM/RTRIMを使用

## 安全制約

### 設計変更の防止
- ❌ インデックスの作成/変更なし（`CreateIndex`, `DropIndex`禁止）
- ❌ CTEの追加なし（`WITH...AS`構文禁止）
- ❌ テーブル構造変更なし（`ALTER TABLE`禁止）
- ❌ 複数最適化の同時実行なし

### 技術制約
- ❌ SQL Server 2017+機能の使用なし
- ❌ CROSS APPLY、OUTER APPLY等の複雑な結合なし
- ❌ パーティショニング関連操作なし

### データ保護
- ✅ すべての変更にSHA2_256検証が必要
- ✅ 検証失敗時の自動ロールバック
- ✅ パフォーマンススナップショットでの完全監査証跡
- ✅ 最小改善率5%の品質保証

## 出力構造

各最適化セッションは構造化されたスナップショットを作成します：

```
performance_snapshots/{viewName}/
├── 00_Baseline/               # 初期状態と分析
├── 01_{ActionType}_{Target}/  # 各最適化ステップ
└── final_report.md           # 包括的な結果レポート
```

## MCPツールの使い方

### 前提条件
1. SQL Serverが起動し、対象データベースにアクセス可能
2. `appsettings.json`の接続文字列が設定済み
3. MCPサーバーが実行中（`dotnet run`または実行ファイル）

### 基本的なワークフロー

#### Step 1: ベースライン分析
```bash
# MCPクライアントから実行
analyze-view-baseline 
  viewIdentifier: "dbo.V_SlowSalesReport"
  snapshotBasePath: "./performance_snapshots/"
```

**取得される情報:**
- ビュー定義SQL
- 実行プラン（XML）
- 現在のパフォーマンス指標（実行時間、IO統計）
- 結果セットSHA2_256チェックサム
- 自動生成された最適化提案リスト

#### Step 2: 個別最適化提案生成
```bash
# 統計情報更新の提案生成
generate-optimization-proposal
  viewName: "dbo.V_SlowSalesReport"
  actionType: "UpdateStatistics"
  targetObject: "dbo.Orders"
  snapshotBasePath: "./performance_snapshots/"

# 不要なDISTINCT削除の提案生成
generate-optimization-proposal
  viewName: "dbo.V_SlowSalesReport" 
  actionType: "RemoveUnnecessaryDistinct"
  snapshotBasePath: "./performance_snapshots/"
```

**各ステップで生成される内容:**
1. 最適化SQL文の自動生成（実行はしない）
2. 元のビュー定義の保存
3. 改善予測効果の算出
4. 提案SQL文の構文・制約検証
5. 実行手順書の生成
6. リスク評価とチェックリスト作成

#### Step 3: 包括的分析・提案生成（推奨）
```bash
# 完全な分析と提案一覧を生成
analyze-and-propose-optimizations
  viewIdentifier: "dbo.V_SlowSalesReport"
  maxProposals: 10
  snapshotBasePath: "./performance_snapshots/"
```

**自動生成される内容:**
1. ベースライン分析
2. 最適化提案の優先度順リスト作成
3. 各提案SQL文の生成（実行はしない）
4. 改善予測効果の算出
5. 実行手順書と最終レポート生成

#### Step 4: 結果確認と検証
```bash
# 現在のパフォーマンス測定
measure-view-performance
  viewName: "dbo.V_SlowSalesReport"
  measurementRuns: 5

# データ整合性確認
validate-view-results
  viewName: "dbo.V_SlowSalesReport"
  baselineChecksum: "A1B2C3D4E5F6..."

# 最終レポート生成
generate-final-report
  viewName: "dbo.V_SlowSalesReport"
  snapshotBasePath: "./performance_snapshots/"
```

### 実用的な使用例

#### 例1: 単発の問題解決
```bash
# 1. 問題のあるビューを特定
analyze-view-baseline viewIdentifier: "dbo.V_CustomerOrders"

# 2. 提案された最優先アクションのSQL生成
generate-optimization-proposal
  viewName: "dbo.V_CustomerOrders"
  actionType: "UpdateStatistics"
  targetObject: "dbo.Customers"

# 3. 現在のパフォーマンス測定（実行前）
measure-view-performance viewName: "dbo.V_CustomerOrders"

# 4. 生成されたSQL文を手動レビュー・実行後、再測定
measure-view-performance viewName: "dbo.V_CustomerOrders"
```

#### 例2: 包括的な分析・提案セッション
```bash
# 包括的分析と提案生成（推奨）
analyze-and-propose-optimizations
  viewIdentifier: "dbo.V_ProductSales"
  maxProposals: 8
  snapshotBasePath: "./snapshots/ProductSales/"
```

**出力される成果物:**
```
snapshots/ProductSales/
├── 00_Baseline/
│   ├── view_definition.sql
│   ├── execution_plan.xml
│   ├── performance_metrics.json
│   └── result_checksum.txt
├── 01_UpdateStatistics_Orders/
│   ├── proposed_sql.sql          # 生成された最適化SQL（実行はしない）
│   ├── execution_guide.md        # 実行手順書
│   ├── risk_assessment.json      # リスク評価
│   └── expected_improvement.json # 改善予測
├── 02_RemoveUnnecessaryDistinct_ProductQuery/
│   ├── proposed_sql.sql
│   ├── before_after_comparison.sql
│   └── validation_checklist.md
└── final_report.md               # 全提案を含む包括レポート
```

### トラブルシューティング

#### 分析・提案生成が失敗する場合
1. **制約違反**: 生成されるSQL文が制約システムに違反
2. **接続エラー**: `appsettings.json`の接続文字列を確認
3. **権限不足**: SELECT権限と実行プラン取得権限が必要
4. **ビュー不存在**: 指定されたビューが存在しない

#### 実際の実行時の注意事項
- **生成されたSQL文のレビュー**: 必ず手動で構文・ロジックを確認
- **テスト環境での事前実行**: 本番環境適用前の動作確認
- **バックアップ取得**: 元のビュー定義の保存
- **段階的実行**: 一度に1つの提案のみ適用

### 制約と注意事項

#### 自動実行される安全チェック
- ✅ 生成SQL文の制約バリデーション
- ✅ 結果セット同一性の予測検証
- ✅ パフォーマンス改善予測の算出
- ✅ 提案SQL文の構文チェック

#### 人間による必須確認事項
- 🔍 **すべての提案SQL文の内容レビュー**
- 🔍 **テスト環境での事前検証**
- 🔍 **本番環境でのバックアップ取得**
- 🔍 **段階的実行（一度に1つずつ）**

## 開発依存関係

- .NET 8.0+
- Microsoft.Data.SqlClient 5.2.2
- ModelContextProtocol 0.3.0-preview.3
- SQL Server 2016+（対象データベース）