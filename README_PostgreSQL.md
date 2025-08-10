# PostgreSQL MCP 伺服器

這是一個 Model Context Protocol (MCP) 伺服器，提供 PostgreSQL 資料庫查詢工具。

## 功能

- 列出資料庫中的所有資料表
- 獲取資料表結構資訊
- 獲取資料表範例資料
- 列出儲存程序（函式）
- 執行只讀查詢

## 設定

### 1. PostgreSQL 資料庫設定

首先確保您有一個執行中的 PostgreSQL 資料庫。您可以：

- 使用本機安裝的 PostgreSQL
- 使用 Docker 執行 PostgreSQL：

```bash
docker run --name postgres-mcp \
  -e POSTGRES_USER=postgres \
  -e POSTGRES_PASSWORD=password \
  -e POSTGRES_DB=testdb \
  -p 5432:5432 \
  -d postgres:15
```

### 2. 設定連線字串

編輯 `appsettings.json` 檔案中的連線字串：

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=testdb;Username=postgres;Password=password;Port=5432;"
  }
}
```

連線字串參數說明：
- `Host`: PostgreSQL 伺服器位址
- `Database`: 資料庫名稱
- `Username`: 使用者名稱
- `Password`: 密碼
- `Port`: 埠號（預設 5432）

### 3. 建構和執行

```bash
dotnet restore
dotnet build
dotnet run
```

## MCP 工具

伺服器提供以下工具：

### list_tables
列出資料庫中的所有資料表。

### get_table_schema
獲取指定資料表的結構資訊。
- 參數: `tableName` - 資料表名稱（可包含 schema，例如 `public.users`）

### get_sample_data
獲取資料表的範例資料。
- 參數: 
  - `tableName` - 資料表名稱
  - `maxRows` - 最大回傳筆數（預設 5）

### list_stored_procedures
列出資料庫中的所有函式。

### execute_query
執行 SELECT 查詢。
- 參數:
  - `query` - SQL 查詢語句
  - `maxRows` - 最大回傳筆數（預設 100）

## 安全性

- 只允許執行 SELECT 查詢
- 會檢查並拒絕包含危險操作的查詢（INSERT、UPDATE、DELETE 等）
- 所有查詢都有行數限制
- 查詢超時時間為 30 秒

## PostgreSQL 特殊功能

- 支援多重 schema
- 預設 schema 為 `public`
- 支援 PostgreSQL 特有的資料型別
- 使用 `information_schema` 和系統資料表獲取 metadata

## 範例資料表建立

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

## 疑難排解

### 連線問題
- 確認 PostgreSQL 服務正在執行
- 檢查防火牆設定
- 確認連線字串正確
- 檢查使用者權限

### 權限問題
確保資料庫使用者有適當的權限：

```sql
-- 授予使用者必要權限
GRANT CONNECT ON DATABASE testdb TO postgres;
GRANT USAGE ON SCHEMA public TO postgres;
GRANT SELECT ON ALL TABLES IN SCHEMA public TO postgres;
```
