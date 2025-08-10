# DbPerformanceMcpServer 配布・導入ガイド

## 📦 **バイナリ配布について**

このMCPサーバーは .NET 8.0 で実装されており、自己完結型のバイナリとして配布・実行できます。.NET Runtimeの事前インストールは不要です。

## 🔧 **バイナリ作成方法**

### 開発者向け：リリース用バイナリ作成

#### 1. **プラットフォーム別ビルド**
```bash
# Windows 64bit用
dotnet publish --configuration Release --runtime win-x64 --self-contained true --output release-win-x64

# Linux 64bit用
dotnet publish --configuration Release --runtime linux-x64 --self-contained true --output release-linux-x64

# macOS 64bit用 (Intel)
dotnet publish --configuration Release --runtime osx-x64 --self-contained true --output release-osx-x64

# macOS ARM64用 (Apple Silicon)
dotnet publish --configuration Release --runtime osx-arm64 --self-contained true --output release-osx-arm64
```

#### 2. **配布パッケージ作成**
```bash
# Windows
cd release-win-x64
zip -r ../DbPerformanceMcpServer-v1.0-win-x64.zip .

# Linux/macOS
cd release-linux-x64
tar -czf ../DbPerformanceMcpServer-v1.0-linux-x64.tar.gz .
```

#### 3. **生成されるファイル構成**
```
release-win-x64/
├── DbPerformanceMcpServer.exe        # メイン実行ファイル
├── appsettings.json                  # 設定ファイル
├── Microsoft.Data.SqlClient.SNI.dll  # SQL Server接続ライブラリ
└── [その他のランタイムファイル]
```

## 🚀 **エンドユーザー向け：導入手順**

### ステップ1: バイナリ配置

#### Windows
```powershell
# 1. 配布パッケージを展開
Expand-Archive -Path DbPerformanceMcpServer-v1.0-win-x64.zip -DestinationPath C:\Tools\DbPerformanceMcp\

# 2. 配置確認
dir C:\Tools\DbPerformanceMcp\
```

#### Linux/macOS
```bash
# 1. 配布パッケージを展開
sudo mkdir -p /usr/local/bin/DbPerformanceMcp
sudo tar -xzf DbPerformanceMcpServer-v1.0-linux-x64.tar.gz -C /usr/local/bin/DbPerformanceMcp/

# 2. 実行権限付与
sudo chmod +x /usr/local/bin/DbPerformanceMcp/DbPerformanceMcpServer

# 3. 配置確認
ls -la /usr/local/bin/DbPerformanceMcp/
```

### ステップ2: 設定ファイル編集

#### SQL Server接続設定
```json
// appsettings.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=YourDatabase;Integrated Security=true;TrustServerCertificate=true;"
  },
  "DbPerformanceOptimizer": {
    "SnapshotBasePath": "./performance_snapshots/",
    "MaxOptimizationSteps": 10,
    "SQL2016Compatible": true
  }
}
```

**接続文字列の例**:
- **Windows認証**: `"Server=localhost;Database=TestDB;Integrated Security=true;TrustServerCertificate=true;"`
- **SQL Server認証**: `"Server=localhost;Database=TestDB;User Id=dbuser;Password=password123;TrustServerCertificate=true;"`
- **リモートサーバー**: `"Server=sql-server.company.com,1433;Database=ProductionDB;User Id=optimizer;Password=SecurePass123;Encrypt=true;TrustServerCertificate=false;"`

### ステップ3: 動作確認テスト

#### 単体動作確認
```bash
# Windows
C:\Tools\DbPerformanceMcp\DbPerformanceMcpServer.exe

# Linux/macOS
/usr/local/bin/DbPerformanceMcp/DbPerformanceMcpServer
```

**正常起動時の出力例**:
```
DbPerformanceMcpServer starting...
MCP server listening on stdio
SQL Server connection: OK
Ready to accept MCP requests
```

## 🔌 **MCPクライアント設定**

### Claude Desktop
```json
// ~/.claude/claude_desktop_config.json (Windows: %APPDATA%\Claude\claude_desktop_config.json)
{
  "mcpServers": {
    "db-performance-optimizer": {
      "command": "C:\\Tools\\DbPerformanceMcp\\DbPerformanceMcpServer.exe",
      "args": [],
      "env": {
        "DOTNET_ENVIRONMENT": "Production"
      }
    }
  }
}
```

### 汎用MCPクライアント
```json
{
  "servers": [
    {
      "name": "db-performance-optimizer",
      "executable": "/usr/local/bin/DbPerformanceMcp/DbPerformanceMcpServer",
      "args": [],
      "env": {
        "DOTNET_ENVIRONMENT": "Production"
      }
    }
  ]
}
```

## 🏢 **企業環境での配布戦略**

### 1. **内部パッケージリポジトリ**
```yaml
# Chocolatey (Windows)
choco pack .\dbperformancemcp.nuspec
choco push dbperformancemcp.1.0.0.nupkg --source internal-repo

# Homebrew (macOS)
brew tap company/internal
brew install dbperformancemcp
```

### 2. **グループポリシー配布 (Windows)**
```powershell
# MSI作成用PowerShellスクリプト例
New-MSIPackage -SourcePath "C:\Build\DbPerformanceMcp" -OutputPath "C:\Packages\DbPerformanceMcp-1.0.0.msi"
```

### 3. **Ansible/Chef/Puppet自動配布**
```yaml
# Ansible Playbook例
- name: Deploy DbPerformanceMcp
  unarchive:
    src: "{{ package_url }}/DbPerformanceMcpServer-v1.0-linux-x64.tar.gz"
    dest: /usr/local/bin/DbPerformanceMcp/
    remote_src: yes
    creates: /usr/local/bin/DbPerformanceMcp/DbPerformanceMcpServer
```

## 🐳 **Docker配布**

### Dockerfile
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY bin/Release/net8.0/linux-x64/publish/ .
EXPOSE 3000
ENTRYPOINT ["./DbPerformanceMcpServer"]
```

### Docker Compose
```yaml
version: '3.8'
services:
  db-performance-mcp:
    image: dbperformancemcp:latest
    volumes:
      - ./appsettings.json:/app/appsettings.json:ro
      - ./performance_snapshots:/app/performance_snapshots
    environment:
      - DOTNET_ENVIRONMENT=Production
    restart: unless-stopped
```

## 🔄 **バージョン管理**

### GitHub Releases
```bash
# タグ作成
git tag -a v1.0.0 -m "Initial release"
git push origin v1.0.0

# GitHub Actions自動ビルド・リリース
# .github/workflows/release.yml で自動化
```

### セマンティックバージョニング
- **Major**: 破壊的変更（v1.0.0 → v2.0.0）
- **Minor**: 機能追加（v1.0.0 → v1.1.0）
- **Patch**: バグ修正（v1.0.0 → v1.0.1）

## 🔒 **セキュリティ考慮事項**

### 1. **ファイルシステム権限**
```bash
# Linux/macOS: 適切な権限設定
sudo chown root:admin /usr/local/bin/DbPerformanceMcp/
sudo chmod 755 /usr/local/bin/DbPerformanceMcp/
sudo chmod 644 /usr/local/bin/DbPerformanceMcp/appsettings.json
```

### 2. **設定ファイル暗号化**
```json
// 接続文字列暗号化の推奨
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=encrypted_config;..."
  }
}
```

### 3. **ファイアウォール設定**
- SQL Server: ポート1433の適切な制限
- 出力フォルダ: 書き込み権限の最小化

## 📊 **監視・ログ**

### ログファイル設定
```json
// appsettings.json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning"
    },
    "File": {
      "Path": "./logs/dbperformancemcp-.log",
      "RollingInterval": "Day"
    }
  }
}
```

### 実行監視
```bash
# systemd (Linux)
sudo systemctl status dbperformancemcp

# Windows Service登録
sc create DbPerformanceMcp binPath="C:\Tools\DbPerformanceMcp\DbPerformanceMcpServer.exe"
```

## 🆘 **トラブルシューティング**

### 1. **接続エラー**
```bash
# SQL Server接続テスト
sqlcmd -S localhost -E -Q "SELECT @@VERSION"

# 設定確認
cat appsettings.json | jq '.ConnectionStrings.DefaultConnection'
```

### 2. **権限エラー**
```bash
# 実行権限確認
ls -la DbPerformanceMcpServer

# SQL Server権限確認
sqlcmd -Q "SELECT SYSTEM_USER, ORIGINAL_LOGIN()"
```

### 3. **依存関係エラー**
```bash
# 依存ファイル確認
ldd DbPerformanceMcpServer  # Linux
otool -L DbPerformanceMcpServer  # macOS
```

## 📞 **サポート**

### 1. **ログ収集**
```bash
# ログファイル確認
tail -f logs/dbperformancemcp-*.log

# 環境情報収集
./DbPerformanceMcpServer --version
```

### 2. **問題報告**
必要な情報:
- OS・バージョン
- SQL Serverバージョン
- エラーメッセージ
- 設定ファイル（接続文字列除く）
- ログファイル

### 3. **更新手順**
```bash
# 新バージョン適用
1. MCPサーバー停止
2. 旧バイナリのバックアップ
3. 新バイナリで置換
4. 設定ファイル確認
5. 動作確認後再開
```

---

**注意**: 本配布ガイドは本番環境での使用を想定しています。セキュリティ要件に応じて設定を調整してください。