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

### データベース接続のテスト
MCPツールを使用する前に、`appsettings.json`の接続文字列が正しいことを確認してください：
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=PMS;Integrated Security=true;TrustServerCertificate=true;"
  }
}
```

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

- ❌ インデックスの作成/変更なし
- ❌ CTEの追加なし 
- ❌ SQL Server 2017+機能の使用なし
- ❌ 複数最適化の同時実行なし
- ✅ すべての変更にSHA2_256検証が必要
- ✅ 検証失敗時の自動ロールバック
- ✅ パフォーマンススナップショットでの完全監査証跡

## 出力構造

各最適化セッションは構造化されたスナップショットを作成します：

```
performance_snapshots/{viewName}/
├── 00_Baseline/               # 初期状態と分析
├── 01_{ActionType}_{Target}/  # 各最適化ステップ
└── final_report.md           # 包括的な結果レポート
```

## 開発依存関係

- .NET 8.0+
- Microsoft.Data.SqlClient 5.2.2
- ModelContextProtocol 0.3.0-preview.3
- SQL Server 2016+（対象データベース）