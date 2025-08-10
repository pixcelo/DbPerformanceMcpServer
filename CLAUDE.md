# CLAUDE.md

このファイルは、Claude Code (claude.ai/code) がこのリポジトリで作業する際のガイダンスを提供します。
必ず日本語で回答してください。

## プロジェクト概要

SQL Serverビューパフォーマンス最適化用のModel Context Protocol (MCP)サーバーです。データの完全な整合性を維持しながら、体系的で安全かつ自動化されたビューパフォーマンス改善を提供します。

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
アプリケーションはクリーンなサービス指向アーキテクチャに従います：

- **IViewAnalysisService**: ベースライン分析とパフォーマンス測定（フェーズ1）
- **IOptimizationService**: 単一ステップ最適化実行と検証（フェーズ2）  
- **IOptimizationOrchestrator**: エンドツーエンド自動化とセッション管理（フェーズ1-3）
- **IValidationService**: SHA2_256チェックサムを使用した結果整合性検証
- **IExecutionPlanAnalyzer**: SQL実行プラン分析と最適化提案
- **ISqlConnectionService**: データベース接続とクエリ実行
- **ISnapshotService**: ファイル出力とパフォーマンススナップショット管理

### 安全第一の設計
- **データ整合性**: SHA2_256ハッシュ検証により結果セットが同一であることを保証
- **自動ロールバック**: 失敗した最適化は自動的に元の状態に戻される
- **段階的実行**: 一度に1つの最適化のみを適用
- **SQL Server 2016互換**: 安定してテスト済みの機能のみを使用

## MCPツール

ビュー最適化のための6つのMCPツールを公開しています：

1. **analyze-view-baseline**: ベースライン分析の実行（実行プラン、パフォーマンス、チェックサム）
2. **execute-optimization-step**: 単一最適化アクションの実行と検証
3. **optimize-view-fully**: 完全自動最適化（フェーズ1-3）
4. **generate-final-report**: 包括的な最適化レポートの生成
5. **measure-view-performance**: 現在のビューパフォーマンスの測定
6. **validate-view-results**: 結果チェックサムの計算/検証

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

#### Step 2: 個別最適化実行
```bash
# 統計情報を更新
execute-optimization-step
  viewName: "dbo.V_SlowSalesReport"
  actionType: "UpdateStatistics"
  targetObject: "dbo.Orders"
  snapshotBasePath: "./performance_snapshots/"

# 不要なDISTINCTを削除
execute-optimization-step
  viewName: "dbo.V_SlowSalesReport" 
  actionType: "RemoveUnnecessaryDistinct"
  snapshotBasePath: "./performance_snapshots/"
```

**各ステップで実行される処理:**
1. 最適化SQL自動生成
2. 元のビュー定義バックアップ
3. 最適化実行
4. 結果セット同一性検証（SHA2_256）
5. パフォーマンス改善測定
6. 失敗時の自動ロールバック

#### Step 3: 完全自動最適化（推奨）
```bash
# フェーズ1-3を自動実行
optimize-view-fully
  viewIdentifier: "dbo.V_SlowSalesReport"
  maxSteps: 10
  snapshotBasePath: "./performance_snapshots/"
```

**自動実行される内容:**
1. ベースライン分析
2. 最適化提案の優先度順実行
3. 各ステップでの整合性検証
4. 改善効果測定と継続判定
5. 最終レポート自動生成

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

# 2. 提案された最優先アクションを実行
execute-optimization-step 
  viewName: "dbo.V_CustomerOrders"
  actionType: "UpdateStatistics"
  targetObject: "dbo.Customers"

# 3. 改善効果を確認
measure-view-performance viewName: "dbo.V_CustomerOrders"
```

#### 例2: 包括的な最適化セッション
```bash
# 完全自動最適化（推奨）
optimize-view-fully
  viewIdentifier: "dbo.V_ProductSales"
  maxSteps: 8
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
│   ├── optimization_sql.sql
│   ├── performance_improvement.json
│   └── validation_result.json
├── 02_RemoveUnnecessaryDistinct_ProductQuery/
└── final_report.md
```

### トラブルシューティング

#### 最適化が失敗する場合
1. **制約違反**: `docs/optimization-constraints.md`で許可される操作を確認
2. **データ不整合**: SHA2_256チェックサムが一致しない場合は自動ロールバック
3. **接続エラー**: `appsettings.json`の接続文字列を確認
4. **権限不足**: `ALTER VIEW`権限とテーブルへの`UPDATE STATISTICS`権限が必要

#### パフォーマンス改善が見られない場合
- 最小改善率5%未満の場合、次のステップに進まない設計
- `maxSteps`を増やして追加の最適化を試行
- 手動で`analyze-view-baseline`を実行して新しい提案を取得

### 制約と注意事項

#### 自動実行される安全チェック
- ✅ 実行前の制約バリデーション
- ✅ 結果セット同一性の厳密検証
- ✅ パフォーマンス改善の定量測定
- ✅ 失敗時の完全自動復旧

#### 手動確認が推奨される場面
- 本番環境での初回実行
- 複雑なビューでの`ConvertSubqueryToJoin`
- 大量データを扱うビューでの統計更新

## 開発依存関係

- .NET 8.0+
- Microsoft.Data.SqlClient 5.2.2
- ModelContextProtocol 0.3.0-preview.3
- SQL Server 2016+（対象データベース）