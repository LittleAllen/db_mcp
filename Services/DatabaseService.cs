using System.Data;
using Microsoft.Data.SqlClient;
using DbMcpServer.Models;

namespace DbMcpServer.Services;

public class DatabaseService
{
    private readonly string _connectionString;

    public DatabaseService(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task&lt;List&lt;string&gt;&gt; GetTablesAsync()
    {
        var tables = new List&lt;string&gt;();
        
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        
        var query = @"
            SELECT TABLE_SCHEMA + '.' + TABLE_NAME as FullName
            FROM INFORMATION_SCHEMA.TABLES 
            WHERE TABLE_TYPE = 'BASE TABLE'
            ORDER BY TABLE_SCHEMA, TABLE_NAME";
            
        using var command = new SqlCommand(query, connection);
        using var reader = await command.ExecuteReaderAsync();
        
        while (await reader.ReadAsync())
        {
            tables.Add(reader.GetString("FullName"));
        }
        
        return tables;
    }

    public async Task&lt;TableSchema&gt; GetTableSchemaAsync(string tableName)
    {
        var parts = tableName.Split('.');
        var schemaName = parts.Length == 2 ? parts[0] : "dbo";
        var tableNameOnly = parts.Length == 2 ? parts[1] : tableName;

        var schema = new TableSchema
        {
            TableName = tableNameOnly,
            SchemaName = schemaName
        };

        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        var query = @"
            SELECT 
                c.COLUMN_NAME,
                c.DATA_TYPE,
                c.IS_NULLABLE,
                c.CHARACTER_MAXIMUM_LENGTH,
                c.COLUMN_DEFAULT,
                CASE WHEN pk.COLUMN_NAME IS NOT NULL THEN 1 ELSE 0 END as IsPrimaryKey,
                CASE WHEN fk.COLUMN_NAME IS NOT NULL THEN 1 ELSE 0 END as IsForeignKey
            FROM INFORMATION_SCHEMA.COLUMNS c
            LEFT JOIN (
                SELECT ku.COLUMN_NAME
                FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
                INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE ku
                    ON tc.CONSTRAINT_NAME = ku.CONSTRAINT_NAME
                WHERE tc.TABLE_SCHEMA = @SchemaName
                    AND tc.TABLE_NAME = @TableName
                    AND tc.CONSTRAINT_TYPE = 'PRIMARY KEY'
            ) pk ON c.COLUMN_NAME = pk.COLUMN_NAME
            LEFT JOIN (
                SELECT ku.COLUMN_NAME
                FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
                INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE ku
                    ON tc.CONSTRAINT_NAME = ku.CONSTRAINT_NAME
                WHERE tc.TABLE_SCHEMA = @SchemaName
                    AND tc.TABLE_NAME = @TableName
                    AND tc.CONSTRAINT_TYPE = 'FOREIGN KEY'
            ) fk ON c.COLUMN_NAME = fk.COLUMN_NAME
            WHERE c.TABLE_SCHEMA = @SchemaName AND c.TABLE_NAME = @TableName
            ORDER BY c.ORDINAL_POSITION";

        using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@SchemaName", schemaName);
        command.Parameters.AddWithValue("@TableName", tableNameOnly);
        
        using var reader = await command.ExecuteReaderAsync();
        
        while (await reader.ReadAsync())
        {
            schema.Columns.Add(new ColumnInfo
            {
                ColumnName = reader.GetString("COLUMN_NAME"),
                DataType = reader.GetString("DATA_TYPE"),
                IsNullable = reader.GetString("IS_NULLABLE") == "YES",
                MaxLength = reader.IsDBNull("CHARACTER_MAXIMUM_LENGTH") ? null : reader.GetInt32("CHARACTER_MAXIMUM_LENGTH"),
                DefaultValue = reader.IsDBNull("COLUMN_DEFAULT") ? null : reader.GetString("COLUMN_DEFAULT"),
                IsPrimaryKey = reader.GetInt32("IsPrimaryKey") == 1,
                IsForeignKey = reader.GetInt32("IsForeignKey") == 1
            });
        }

        return schema;
    }

    public async Task&lt;List&lt;StoredProcedureInfo&gt;&gt; GetStoredProceduresAsync()
    {
        var procedures = new List&lt;StoredProcedureInfo&gt;();
        
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        
        var query = @"
            SELECT 
                ROUTINE_SCHEMA,
                ROUTINE_NAME,
                ROUTINE_DEFINITION
            FROM INFORMATION_SCHEMA.ROUTINES 
            WHERE ROUTINE_TYPE = 'PROCEDURE'
            ORDER BY ROUTINE_SCHEMA, ROUTINE_NAME";
            
        using var command = new SqlCommand(query, connection);
        using var reader = await command.ExecuteReaderAsync();
        
        while (await reader.ReadAsync())
        {
            procedures.Add(new StoredProcedureInfo
            {
                SchemaName = reader.GetString("ROUTINE_SCHEMA"),
                Name = reader.GetString("ROUTINE_NAME"),
                Definition = reader.IsDBNull("ROUTINE_DEFINITION") ? null : reader.GetString("ROUTINE_DEFINITION")
            });
        }
        
        return procedures;
    }

    public async Task&lt;DataTable&gt; ExecuteQueryAsync(string query, int maxRows = 100)
    {
        if (ContainsUnsafeOperations(query))
        {
            throw new InvalidOperationException("Query contains potentially unsafe operations");
        }

        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        
        var safeQuery = $"SELECT TOP ({maxRows}) * FROM ({query}) AS SubQuery";
        
        using var command = new SqlCommand(safeQuery, connection);
        command.CommandTimeout = 30;
        
        using var adapter = new SqlDataAdapter(command);
        var dataTable = new DataTable();
        adapter.Fill(dataTable);
        
        return dataTable;
    }

    public async Task&lt;DataTable&gt; GetSampleDataAsync(string tableName, int maxRows = 5)
    {
        var parts = tableName.Split('.');
        var schemaName = parts.Length == 2 ? parts[0] : "dbo";
        var tableNameOnly = parts.Length == 2 ? parts[1] : tableName;

        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        
        var query = $"SELECT TOP ({maxRows}) * FROM [{schemaName}].[{tableNameOnly}]";
        
        using var command = new SqlCommand(query, connection);
        using var adapter = new SqlDataAdapter(command);
        var dataTable = new DataTable();
        adapter.Fill(dataTable);
        
        return dataTable;
    }

    private static bool ContainsUnsafeOperations(string query)
    {
        var unsafeKeywords = new[] { "DELETE", "INSERT", "UPDATE", "DROP", "CREATE", "ALTER", "EXEC", "EXECUTE" };
        var upperQuery = query.ToUpper();
        
        return unsafeKeywords.Any(keyword => upperQuery.Contains(keyword));
    }
}