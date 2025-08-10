# 最適化制約システム

DbPerformanceMcpServerは、安全で制御された最適化を実現するため、包括的な制約システムを搭載しています。このドキュメントでは、制約の設定方法と動作について詳しく説明します。

## 📋 制約システムの目的

### 問題の背景
- **設計変更の回避**: インデックス作成など、データベース設計に影響する変更を防ぐ
- **ソースコード制限**: CTEなど、呼び出し元コードで対応困難な構文を避ける
- **リスク管理**: 過度に複雑な変更や実行時間の長い最適化を制限
- **品質保証**: 十分な改善効果のない変更を排除

### 制約の分類
1. **アクション制約** - 実行可能な最適化アクションの制限
2. **SQL構文制約** - 生成されるSQLコードのパターン制限
3. **ビュー定義制約** - ビュー変更時の構造制限
4. **パフォーマンス制約** - 改善効果と実行時間の基準

## ⚙️ 設定構造

### appsettings.json の制約設定
```json
{
  "DbPerformanceOptimizer": {
    "Constraints": {
      "ForbiddenActions": [
        "CreateIndex",
        "DropIndex", 
        "CreateCTE",
        "AddPartitioning",
        "CreateView",
        "AlterTableStructure"
      ],
      "AllowedActions": [
        "UpdateStatistics",
        "RemoveUnnecessaryDistinct",
        "ConvertSubqueryToJoin",
        "FixImplicitConversion",
        "OptimizeStringConcatenation",
        "PrecomputeCalculatedColumns",
        "RemoveUnnecessarySort",
        "OptimizeStringOperations"
      ],
      "ForbiddenSqlPatterns": [
        "CREATE INDEX",
        "DROP INDEX",
        "WITH.*AS.*\\(",
        "PARTITION BY",
        "ALTER TABLE.*ADD",
        "ALTER TABLE.*DROP"
      ],
      "ForbiddenViewPatterns": [
        "WITH.*AS.*\\(",
        "CREATE.*VIEW",
        "UNION ALL",
        "CROSS APPLY",
        "OUTER APPLY"
      ],
      "MinimumImprovementPercentage": 5.0,
      "MaxExecutionTimeMs": 30000,
      "MaxViewDefinitionLength": 50000
    }
  }
}
```

## 🔍 制約の詳細説明

### 1. ForbiddenActions（禁止アクション）
設計変更を伴う危険なアクションを完全に禁止します。

| アクション | 理由 | 影響範囲 |
|-----------|------|----------|
| `CreateIndex` | DB設計変更、権限要求 | テーブル構造 |
| `DropIndex` | データアクセス性能劣化リスク | クエリ性能全般 |
| `CreateCTE` | 呼び出し元コード対応困難 | アプリケーション層 |
| `AddPartitioning` | 大規模な構造変更 | テーブル全体 |
| `CreateView` | 新規オブジェクト作成 | データベーススキーマ |
| `AlterTableStructure` | テーブル定義変更 | データ整合性 |

### 2. AllowedActions（許可アクション）
ホワイトリスト方式で安全な最適化のみを許可します。

| アクション | 説明 | 安全性 |
|-----------|------|-------|
| `UpdateStatistics` | 統計情報の更新 | 高（データ不変） |
| `RemoveUnnecessaryDistinct` | 冗長DISTINCT削除 | 高（結果同一） |
| `ConvertSubqueryToJoin` | サブクエリ→JOIN変換 | 中（要検証） |
| `FixImplicitConversion` | 型変換最適化 | 高（性能向上） |
| `OptimizeStringConcatenation` | 文字列操作改善 | 高（SQL2016互換） |
| `PrecomputeCalculatedColumns` | 計算結果事前計算 | 中（複雑度上昇） |
| `RemoveUnnecessarySort` | 冗長ソート削除 | 高（性能向上） |
| `OptimizeStringOperations` | LTRIM/RTRIM使用 | 高（標準関数） |

### 3. ForbiddenSqlPatterns（禁止SQLパターン）
正規表現で危険なSQL構文をブロックします。

```sql
-- ❌ 禁止パターンの例
CREATE INDEX ix_example ON table1 (col1);  -- インデックス作成
WITH cte AS (SELECT ...) SELECT * FROM cte; -- CTE使用
ALTER TABLE table1 ADD COLUMN new_col INT;  -- テーブル構造変更
```

### 4. ForbiddenViewPatterns（禁止ビューパターン）
ビュー定義で避けるべき複雑な構文をブロックします。

```sql
-- ❌ 禁止されるビュー定義
CREATE VIEW v_example AS
WITH regional_sales AS (  -- CTE使用
    SELECT region, SUM(sales) as total
    FROM sales_data
    GROUP BY region
)
SELECT * FROM regional_sales
CROSS APPLY (SELECT TOP 1 * FROM details) d;  -- CROSS APPLY使用
```

### 5. パフォーマンス制約

#### MinimumImprovementPercentage（最小改善率）
```json
"MinimumImprovementPercentage": 5.0  // 5%未満の改善は無効
```

改善効果の計算：
```
改善率 = (改善前実行時間 - 改善後実行時間) / 改善前実行時間 × 100
```

#### MaxExecutionTimeMs（最大実行時間）
```json
"MaxExecutionTimeMs": 30000  // 30秒を超える実行は危険と判定
```

#### MaxViewDefinitionLength（最大ビュー定義長）
```json
"MaxViewDefinitionLength": 50000  // 50,000文字を超える定義は禁止
```

## 🚨 違反処理レベル

制約違反は重要度に応じて段階的に処理されます：

### Critical（致命的）
- **動作**: 実行を即座に停止
- **対象**: ForbiddenActions、危険なSQL構文
- **例**: インデックス作成の試行

### Error（エラー）
- **動作**: 実行停止、詳細ログ出力
- **対象**: 許可リスト外のアクション、実行時間超過
- **例**: 30秒を超える最適化の試行

### Warning（警告）
- **動作**: 実行継続、注意喚起
- **対象**: 改善効果不足
- **例**: 3%の改善率（5%未満）

## 🔧 制約検証の実行タイミング

### 1. 事前検証（Pre-validation）
```
実行前チェック → アクション許可判定 → SQL構文検証 → 実行可否決定
```

### 2. 実行中検証（Runtime validation）
```
SQL生成 → パターンマッチング → ビュー定義チェック → 実行許可
```

### 3. 事後検証（Post-validation）
```
実行完了 → 改善率計算 → 実行時間チェック → 結果有効性判定
```

## 📝 制約カスタマイズ例

### ケース1: より厳格な制約
```json
{
  "Constraints": {
    "AllowedActions": [
      "UpdateStatistics",
      "RemoveUnnecessaryDistinct"
    ],
    "MinimumImprovementPercentage": 10.0,
    "MaxExecutionTimeMs": 15000,
    "ForbiddenViewPatterns": [
      "WITH.*AS.*\\(",
      "UNION",
      "CROSS APPLY", 
      "OUTER APPLY",
      "PIVOT",
      "UNPIVOT"
    ]
  }
}
```

### ケース2: 開発環境用の緩い制約
```json
{
  "Constraints": {
    "MinimumImprovementPercentage": 1.0,
    "MaxExecutionTimeMs": 60000,
    "ForbiddenActions": [
      "CreateIndex",
      "DropIndex"
    ]
  }
}
```

### ケース3: 特定パターンのみ許可
```json
{
  "Constraints": {
    "AllowedActions": [
      "UpdateStatistics",
      "FixImplicitConversion"
    ],
    "ForbiddenSqlPatterns": [
      "CREATE",
      "DROP", 
      "ALTER",
      "WITH.*AS.*\\("
    ]
  }
}
```

## 🐛 トラブルシューティング

### よくある制約違反とその対処

#### 1. 「アクションが禁止されています」
```
エラー: アクション 'CreateCTE' は禁止されています（設計変更を伴う可能性があるため）
対処: AllowedActionsリストに追加するか、代替アクションを使用
```

#### 2. 「禁止されたSQLパターンが検出されました」
```
エラー: 禁止されたSQLパターンが検出されました: WITH.*AS.*\(
対処: ForbiddenSqlPatternsからパターンを除去するか、別の構文を使用
```

#### 3. 「改善効果が不十分です」
```
警告: 改善効果が不十分です (実際: 2.34%, 期待: 5.00%以上)
対処: MinimumImprovementPercentageを調整するか、より効果的な最適化を実行
```

#### 4. 「実行時間が許容範囲を超えています」
```
エラー: 実行時間が許容範囲を超えています (実際: 45000ms, 期待: 30000ms以下)
対処: MaxExecutionTimeMsを増加するか、より軽量な最適化を選択
```

### 制約設定のデバッグ

#### ログレベル設定
```json
{
  "Logging": {
    "LogLevel": {
      "DbPerformanceMcpServer.Services.Implementations.ConstraintValidator": "Debug"
    }
  }
}
```

#### 制約チェックの無効化（テスト用）
```json
{
  "Constraints": {
    "ForbiddenActions": [],
    "AllowedActions": [],
    "ForbiddenSqlPatterns": [],
    "MinimumImprovementPercentage": 0.0,
    "MaxExecutionTimeMs": 999999999
  }
}
```

## 🎯 ベストプラクティス

### 1. 段階的な制約緩和
- 初期は厳格な制約でスタート
- 運用しながら必要に応じて制約を緩和
- 本番環境では保守的な設定を維持

### 2. 環境別設定
- **開発**: 緩い制約、詳細ログ有効
- **テスト**: 本番相当の制約、検証強化
- **本番**: 最も厳格な制約、最小限のリスク

### 3. 定期的な制約見直し
- 制約違反ログの分析
- 改善提案の効果測定
- 新しい脅威パターンの追加

このシステムにより、安全で予測可能なデータベース最適化が実現されます。