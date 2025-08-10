# Database MCP Server 設定指南

支援 VS Code、Claude Desktop 和 Claude Code 三種平台

## VS Code 設定方式

### 1. Workspace 配置（推薦）

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

### 2. User 配置（全域設定）

在 VS Code 中：
1. 開啟 Command Palette (`Ctrl+Shift+P`)
2. 執行 `MCP: Open User Configuration`
3. 加入相同的 JSON 配置

### 3. 啟用 Agent Mode

1. 開啟 Chat 視窗 (`Ctrl+Alt+I`)
2. 在下拉選單中選擇 **Agent mode**
3. 點選 **Tools** 按鈕查看可用工具
4. 選取您的 database-mcp 工具

## Claude Desktop 設定方式

### 配置檔位置

**Windows:** `%APPDATA%\Claude\claude_desktop_config.json`
**macOS:** `~/Library/Application Support/Claude/claude_desktop_config.json`
**Linux:** `~/.config/claude/claude_desktop_config.json`

### 配置內容

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

## Claude Code 設定方式

Claude Code 提供最簡潔的 MCP Server 管理方式。

### 1. 新增 MCP Server

```bash
# 在專案根目錄執行
claude mcp add database-mcp dotnet run --project DbMcpServer.csproj

# 或指定完整路徑
claude mcp add database-mcp --cwd /path/to/DbMcpServer dotnet run --project DbMcpServer.csproj
```

### 2. 管理指令

```bash
# 列出所有 MCP Servers
claude mcp list

# 查看特定 Server 詳細資訊
claude mcp get database-mcp

# 移除 MCP Server
claude mcp remove database-mcp

# 檢查 Server 狀態
/mcp
```

### 3. 配置範圍

Claude Code 支援三種配置範圍：

- **Local scope**: 個人專案專用
- **Project scope**: 團隊共用（`.mcp.json`）
- **User scope**: 跨專案使用

### 4. 使用方式

MCP Server 新增後，直接在對話中使用：

```
請列出資料庫中的所有表格
請顯示 Users 表格的結構資訊
請提供 Products 表格的範例資料
執行查詢: SELECT * FROM Orders WHERE Status = 'Pending'
```

## 使用需求

1. **VS Code 1.101+** (支援遠端 MCP 和 OAuth)
2. **GitHub Copilot Extension** 最新版
3. **Claude Code CLI** 最新版
4. **.NET 8.0+** Runtime
5. **有效的資料庫連線**

## 使用步驟

1. **確認 MCP Server 運作**
   ```bash
   cd DbMcpServer
   dotnet build
   dotnet run
   ```

2. **在各平台中啟用**

   **VS Code:**
   - 開啟 Agent mode
   - 確認工具列表中顯示 database-mcp

   **Claude Code:**
   - 執行 `claude mcp add database-mcp dotnet run --project DbMcpServer.csproj`
   - 直接在對話中使用資料庫功能

   **Claude Desktop:**
   - 重新啟動 Claude Desktop
   - 確認 MCP Server 連線成功

3. **測試功能**

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

### 連線問題
- 確認資料庫服務運作正常
- 檢查防火牆設定
- 驗證資料庫權限

## 安全注意事項

- MCP Server 僅執行只讀操作
- 所有工具執行前會要求確認
- 查詢結果自動限制數量
- 連線逾時保護機制