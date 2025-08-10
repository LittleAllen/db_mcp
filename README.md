# PostgreSQL MCP 伺服器

一個 Model Context Protocol (MCP) 伺服器，為 AI 代理提供安全、結構化的 PostgreSQL 資料庫存取功能。

## 功能

- **結構描述內省**: 獲取資料表結構、欄位資訊和關聯性
- **安全查詢執行**: 執行唯讀 SELECT 查詢，內建安全檢查
- **範例資料存取**: 從資料表中檢索範例資料以提供上下文
- **儲存程序探索**: 列出和檢查儲存的函式
- **安全優先設計**: 防止不安全的操作（INSERT、UPDATE、DELETE）

## 可用工具

1. `list_tables` - 列出資料庫中的所有資料表
2. `get_table_schema` - 獲取特定資料表的詳細結構描述資訊
3. `get_sample_data` - 從資料表檢索範例資料
4. `list_stored_procedures` - 列出所有儲存函式
5. `execute_query` - 執行唯讀 SELECT 查詢

## 系統需求

- .NET 8.0 或更高版本
- PostgreSQL 資料庫
- 適當的資料庫讀取權限

## 快速開始

### 1. 資料庫設定

在 `appsettings.json` 中設定您的 PostgreSQL 連線字串：

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=testdb;Username=postgres;Password=password;Port=5432;"
  }
}
```

### 2. 執行伺服器

```powershell
dotnet restore
dotnet build
dotnet run
```

## 平台設定指南

此 MCP 伺服器支援 VS Code、Claude Desktop 和 Claude Code 三種平台。

### VS Code 設定

#### 1. Workspace 配置（推薦）

在專案根目錄建立 `.vscode/mcp.json`：

```json
{
  "servers": {
    "database-mcp": {
      "type": "local",
      "command": "dotnet",
      "args": ["run", "--project", "DbMcpServer.csproj"],
      "cwd": "${workspaceFolder}",
      "env": {
        "DOTNET_ENVIRONMENT": "Development"
      }
    }
  }
}
```

#### 2. 啟用 Agent Mode

1. 開啟 Chat 視窗 (`Ctrl+Alt+I`)
2. 在下拉選單中選擇 **Agent mode**
3. 點選 **Tools** 按鈕查看可用工具
4. 選取您的 database-mcp 工具

### Claude Desktop 設定

#### 配置檔位置

- **Windows:** `%APPDATA%\Claude\claude_desktop_config.json`
- **macOS:** `~/Library/Application Support/Claude/claude_desktop_config.json`
- **Linux:** `~/.config/claude/claude_desktop_config.json`

#### 配置內容

```json
{
  "mcpServers": {
    "database-mcp": {
      "command": "dotnet",
      "args": ["run", "--project", "DbMcpServer.csproj"],
      "cwd": "/path/to/your/DbMcpServer"
    }
  }
}
```

### Claude Code 設定

```bash
# 新增 MCP Server
claude mcp add database-mcp dotnet run --project DbMcpServer.csproj

# 列出所有 MCP Servers
claude mcp list

# 檢查 Server 狀態
/mcp
```

## 跨專案部署

### 方案 1：發布為可執行檔案（推薦）

#### 步驟 1：發布專案
```powershell
cd c:\Study\mcp\db_mcp
dotnet publish -c Release -r win-x64 --self-contained false
```

#### 步驟 2：設定 MCP 伺服器

##### 選項 A：使用者全域設定（適合多專案同一Database）

在 VS Code 使用者設定中配置，讓所有專案都能使用：

**檔案位置：** `%APPDATA%\Code\User\mcp.json` (Windows)

```json
{
  "servers": {
    "database-mcp": {
      "command": "c:\\Study\\mcp\\db_mcp\\bin\\Release\\net8.0\\win-x64\\publish\\DbMcpServer.exe",
      "args": [],
      "env": {
        "CONNECTION_STRING": "Host=localhost;Database=your_default_db;Username=postgres;Password=yourpassword;Port=5432;",
        "DOTNET_ENVIRONMENT": "Production"
      }
    }
  }
}
```

**優點：**
- 無需在每個專案設定
- 所有 VS Code 工作區自動可用
- 統一的資料庫存取設定

##### 選項 B：專案工作區設定（適合每個專案都有個別的Database）

在專案根目錄建立 `.vscode/mcp.json`：

**使用環境變數：**
```json
{
  "servers": {
    "database-mcp": {
      "command": "c:\\Study\\mcp\\db_mcp\\bin\\Release\\net8.0\\win-x64\\publish\\DbMcpServer.exe",
      "args": [],
      "env": {
        "CONNECTION_STRING": "Host=localhost;Database=myproject_db;Username=postgres;Password=mypassword;Port=5432;",
        "DOTNET_ENVIRONMENT": "Production"
      }
    }
  }
}
```

**使用自訂設定檔案：**
```json
{
  "servers": {
    "database-mcp": {
      "command": "c:\\Study\\mcp\\db_mcp\\bin\\Release\\net8.0\\win-x64\\publish\\DbMcpServer.exe",
      "args": ["--config=c:\\MyProject\\database-config.json"]
    }
  }
}
```

**優點：**
- 每個專案可有不同的資料庫設定
- 設定隨專案版本控制
- 適合需要連接不同資料庫的專案

### 方案 2：全域工具安裝

#### 步驟 1：建立 NuGet 套件
修改 `DbMcpServer.csproj` 加入：
```xml
<PropertyGroup>
  <PackAsTool>true</PackAsTool>
  <ToolCommandName>db-mcp-server</ToolCommandName>
  <GeneratePackageOnBuild>true</GeneratePackageOnBuild>
  <PackageId>DatabaseMcpServer</PackageId>
  <Version>1.0.0</Version>
</PropertyGroup>
```

#### 步驟 2：安裝工具
```powershell
dotnet pack -c Release
dotnet tool install --global --add-source ./bin/Release DatabaseMcpServer
```

#### 步驟 3：在其他專案中使用
```json
{
  "servers": {
    "database-mcp": {
      "command": "db-mcp-server",
      "args": []
    }
  }
}
```

### 方案 3：開發模式（絕對路徑）

```json
{
  "servers": {
    "database-mcp": {
      "command": "dotnet",
      "args": ["run", "--project", "c:\\Study\\mcp\\db_mcp\\DbMcpServer.csproj"],
      "env": {
        "DOTNET_ENVIRONMENT": "Development"
      }
    }
  }
}
```

### 連線字串優先級
1. `CONNECTION_STRING` 環境變數（最高優先級）
2. 使用 `--config=path` 參數指定的設定檔案
3. 可執行檔案目錄下的 `appsettings.json`
4. 若都未設定則拋出錯誤

## PostgreSQL 特定功能

- 支援多重 schema（預設為 `public`）
- 支援 PostgreSQL 特有的資料型別
- 使用 PostgreSQL 系統資料表和 information_schema
- 參數化查詢防止 SQL 注入

### PostgreSQL 範例資料表建立

```sql
-- 建立範例資料表
CREATE TABLE public.users (
    id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    email VARCHAR(255) UNIQUE NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE public.orders (
    id SERIAL PRIMARY KEY,
    user_id INTEGER REFERENCES users(id),
    amount DECIMAL(10,2) NOT NULL,
    status VARCHAR(20) DEFAULT 'pending',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- 插入範例資料
INSERT INTO public.users (name, email) VALUES 
('張三', 'zhang@example.com'),
('李四', 'li@example.com'),
('王五', 'wang@example.com');

INSERT INTO public.orders (user_id, amount, status) VALUES 
(1, 99.99, 'completed'),
(2, 149.50, 'pending'),
(1, 79.99, 'completed');
```

## 使用範例

設定完成後，可以在各平台中使用以下方式與資料庫互動：

**VS Code (Agent mode):**
```
@agent 請幫我列出資料庫中的所有表格
@agent 請顯示 Users 表格的結構
@agent 請提供 Products 表格的範例資料
```

**Claude Code & Claude Desktop:**
```
請列出資料庫中的所有表格
請顯示 Users 表格的完整結構
請提供 Orders 表格的前5筆範例資料
執行查詢: SELECT * FROM Orders WHERE Status = 'Pending'
```

## 疑難排解

### MCP Server 無法啟動
- 檢查 .NET Runtime 是否安裝
- 驗證資料庫連線字串
- 查看對應平台的錯誤訊息：
  - VS Code: 輸出面板
  - Claude Code: 終端機
  - Claude Desktop: 應用程式日誌

### 工具未顯示
- **VS Code**: 確認 Agent mode 已啟用，檢查 mcp.json 語法
- **Claude Code**: 使用 `claude mcp list` 確認 Server 已註冊
- **Claude Desktop**: 重新啟動應用程式，檢查配置檔格式

### 資料庫連線問題
- 確認 PostgreSQL 服務正在執行
- 檢查防火牆設定
- 確認連線字串正確
- 檢查使用者權限

確保資料庫使用者有適當的權限：

```sql
-- 授予使用者必要權限
GRANT CONNECT ON DATABASE testdb TO postgres;
GRANT USAGE ON SCHEMA public TO postgres;
GRANT SELECT ON ALL TABLES IN SCHEMA public TO postgres;
```

## 安全功能

- **只允許 SELECT 查詢**: 自動檢查並拒絕包含危險操作的查詢（INSERT、UPDATE、DELETE 等）
- **查詢結果限制**: 防止過量資料檢索，預設最多返回 100 筆記錄
- **連線逾時保護**: 查詢超時時間為 30 秒，防止長時間執行的操作
- **參數化查詢**: 防止 SQL 注入攻擊
- **輸入驗證**: 對所有輸入進行安全檢查
- **只讀存取**: MCP Server 僅執行只讀操作，確保資料安全

## 部署建議

- **多專案共用Database**: 使用方案 1 + 使用者全域設定，一次設定全專案可用
- **每個專案獨立Database**: 使用方案 1 + 專案工作區設定，可針對不同專案設定不同資料庫
- **開發階段**: 使用方案 3（絕對路徑），方便除錯
- **生產環境**: 使用方案 1（發布可執行檔案），效能最佳
- **多機器部署**: 使用方案 2（全域工具），便於維護
- **團隊協作**: 使用環境變數設定連線字串，避免敏感資訊洩露

## 版本需求

- **VS Code**: 1.101+ (支援遠端 MCP 和 OAuth)
- **GitHub Copilot Extension**: 最新版
- **Claude Code CLI**: 最新版
- **.NET Runtime**: 8.0+
- **PostgreSQL**: 12+