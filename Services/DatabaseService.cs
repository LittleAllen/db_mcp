using System.Data;
using Npgsql;
using DbMcpServer.Models;

namespace DbMcpServer.Services;

public class DatabaseService
{
    private readonly string _connectionString;

    public DatabaseService(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<List<string>> GetTablesAsync()
    {
        var tables = new List<string>();
        
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        
        var query = @"
            SELECT schemaname || '.' || tablename as full_name
            FROM pg_tables 
            WHERE schemaname NOT IN ('information_schema', 'pg_catalog')
            ORDER BY schemaname, tablename";
            
        using var command = new NpgsqlCommand(query, connection);
        using var reader = await command.ExecuteReaderAsync();
        
        while (await reader.ReadAsync())
        {
            tables.Add(reader.GetString("full_name"));
        }
        
        return tables;
    }

    public async Task<TableSchema> GetTableSchemaAsync(string tableName)
    {
        var parts = tableName.Split('.');
        var schemaName = parts.Length == 2 ? parts[0] : "public";
        var tableNameOnly = parts.Length == 2 ? parts[1] : tableName;

        var schema = new TableSchema
        {
            TableName = tableNameOnly,
            SchemaName = schemaName
        };

        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var query = @"
            SELECT 
                c.column_name,
                c.data_type,
                c.is_nullable,
                c.character_maximum_length,
                c.column_default,
                CASE WHEN pk.column_name IS NOT NULL THEN true ELSE false END as is_primary_key,
                CASE WHEN fk.column_name IS NOT NULL THEN true ELSE false END as is_foreign_key
            FROM information_schema.columns c
            LEFT JOIN (
                SELECT kcu.column_name
                FROM information_schema.table_constraints tc
                JOIN information_schema.key_column_usage kcu 
                    ON tc.constraint_name = kcu.constraint_name
                    AND tc.table_schema = kcu.table_schema
                WHERE tc.table_schema = $1
                    AND tc.table_name = $2
                    AND tc.constraint_type = 'PRIMARY KEY'
            ) pk ON c.column_name = pk.column_name
            LEFT JOIN (
                SELECT kcu.column_name
                FROM information_schema.table_constraints tc
                JOIN information_schema.key_column_usage kcu 
                    ON tc.constraint_name = kcu.constraint_name
                    AND tc.table_schema = kcu.table_schema
                WHERE tc.table_schema = $1
                    AND tc.table_name = $2
                    AND tc.constraint_type = 'FOREIGN KEY'
            ) fk ON c.column_name = fk.column_name
            WHERE c.table_schema = $1 AND c.table_name = $2
            ORDER BY c.ordinal_position";

        using var command = new NpgsqlCommand(query, connection);
        command.Parameters.AddWithValue(schemaName);
        command.Parameters.AddWithValue(tableNameOnly);
        
        using var reader = await command.ExecuteReaderAsync();
        
        while (await reader.ReadAsync())
        {
            schema.Columns.Add(new ColumnInfo
            {
                ColumnName = reader.GetString("column_name"),
                DataType = reader.GetString("data_type"),
                IsNullable = reader.GetString("is_nullable") == "YES",
                MaxLength = reader.IsDBNull("character_maximum_length") ? null : reader.GetInt32("character_maximum_length"),
                DefaultValue = reader.IsDBNull("column_default") ? null : reader.GetString("column_default"),
                IsPrimaryKey = reader.GetBoolean("is_primary_key"),
                IsForeignKey = reader.GetBoolean("is_foreign_key")
            });
        }

        return schema;
    }

    public async Task<List<StoredProcedureInfo>> GetStoredProceduresAsync()
    {
        var procedures = new List<StoredProcedureInfo>();
        
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        
        var query = @"
            SELECT 
                routine_schema,
                routine_name,
                routine_definition
            FROM information_schema.routines 
            WHERE routine_type = 'FUNCTION'
                AND routine_schema NOT IN ('information_schema', 'pg_catalog')
            ORDER BY routine_schema, routine_name";
            
        using var command = new NpgsqlCommand(query, connection);
        using var reader = await command.ExecuteReaderAsync();
        
        while (await reader.ReadAsync())
        {
            procedures.Add(new StoredProcedureInfo
            {
                SchemaName = reader.GetString("routine_schema"),
                Name = reader.GetString("routine_name"),
                Definition = reader.IsDBNull("routine_definition") ? null : reader.GetString("routine_definition")
            });
        }
        
        return procedures;
    }

    public async Task<DataTable> ExecuteQueryAsync(string query, int maxRows = 100)
    {
        if (ContainsUnsafeOperations(query))
        {
            throw new InvalidOperationException("Query contains potentially unsafe operations");
        }

        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        
        var safeQuery = $"SELECT * FROM ({query}) AS SubQuery LIMIT {maxRows}";
        
        using var command = new NpgsqlCommand(safeQuery, connection);
        command.CommandTimeout = 30;
        
        using var adapter = new NpgsqlDataAdapter(command);
        var dataTable = new DataTable();
        adapter.Fill(dataTable);
        
        return dataTable;
    }

    public async Task<DataTable> GetSampleDataAsync(string tableName, int maxRows = 5)
    {
        var parts = tableName.Split('.');
        var schemaName = parts.Length == 2 ? parts[0] : "public";
        var tableNameOnly = parts.Length == 2 ? parts[1] : tableName;

        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        
        var query = $"SELECT * FROM \"{schemaName}\".\"{tableNameOnly}\" LIMIT {maxRows}";
        
        using var command = new NpgsqlCommand(query, connection);
        using var adapter = new NpgsqlDataAdapter(command);
        var dataTable = new DataTable();
        adapter.Fill(dataTable);
        
        return dataTable;
    }

    private static bool ContainsUnsafeOperations(string query)
    {
        var unsafeKeywords = new[] { "DELETE", "INSERT", "UPDATE", "DROP", "CREATE", "ALTER", "EXEC", "EXECUTE", "CALL", "TRUNCATE" };
        var upperQuery = query.ToUpper();
        
        return unsafeKeywords.Any(keyword => upperQuery.Contains(keyword));
    }
}
