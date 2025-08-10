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

## 設定

在 `appsettings.json` 中更新您的 PostgreSQL 資料庫連線字串：

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=testdb;Username=postgres;Password=password;Port=5432;"
  }
}
```

## 執行伺服器

```bash
dotnet restore
dotnet build
dotnet run
```

## 安全功能

- 只允許 SELECT 查詢
- 查詢結果有限制，防止過量資料檢索
- 連線逾時防止長時間執行的操作
- 輸入驗證防止 SQL 注入

## 與 AI 客戶端的使用

設定您的 AI 客戶端（如 Claude Desktop）連接到此 MCP 伺服器，以獲得資料庫分析和查詢功能。

## PostgreSQL 特定功能

- 支援多重 schema（預設為 `public`）
- 支援 PostgreSQL 特有的資料型別
- 使用 PostgreSQL 系統資料表和 information_schema
- 參數化查詢防止 SQL 注入

詳細設定指引請參閱 `README_PostgreSQL.md`。

## Requirements

- .NET 8.0 or higher
- SQL Server database
- Appropriate database permissions for read access