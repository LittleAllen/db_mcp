using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using DbMcpServer.Services;
using Microsoft.Extensions.Logging;

namespace DbMcpServer;

[McpServerToolType]
public class DatabaseMcpServer
{
    private readonly DatabaseService _databaseService;
    private readonly ILogger<DatabaseMcpServer> _logger;

    public DatabaseMcpServer(DatabaseService databaseService, ILogger<DatabaseMcpServer> logger)
    {
        _databaseService = databaseService;
        _logger = logger;
    }

    [McpServerTool, Description("List all tables in the database")]
    public async Task<string> ListTables()
    {
        try
        {
            var tables = await _databaseService.GetTablesAsync();
            var result = JsonSerializer.Serialize(new { tables }, new JsonSerializerOptions { WriteIndented = true });
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing tables");
            throw;
        }
    }

    [McpServerTool, Description("Get the schema information for a specific table")]
    public async Task<string> GetTableSchema([Description("Name of the table (can include schema: schema.table)")] string tableName)
    {
        try
        {
            if (string.IsNullOrEmpty(tableName))
                throw new ArgumentException("table_name parameter is required");

            var schema = await _databaseService.GetTableSchemaAsync(tableName);
            var result = JsonSerializer.Serialize(schema, new JsonSerializerOptions { WriteIndented = true });
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting table schema for {TableName}", tableName);
            throw;
        }
    }

    [McpServerTool, Description("Get sample data from a table")]
    public async Task<string> GetSampleData(
        [Description("Name of the table")] string tableName,
        [Description("Maximum number of rows to return (default: 5)")] int maxRows = 5)
    {
        try
        {
            if (string.IsNullOrEmpty(tableName))
                throw new ArgumentException("table_name parameter is required");

            var dataTable = await _databaseService.GetSampleDataAsync(tableName, maxRows);
            var result = ConvertDataTableToJson(dataTable);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sample data for {TableName}", tableName);
            throw;
        }
    }

    [McpServerTool, Description("List all stored procedures in the database")]
    public async Task<string> ListStoredProcedures()
    {
        try
        {
            var procedures = await _databaseService.GetStoredProceduresAsync();
            var result = JsonSerializer.Serialize(new { stored_procedures = procedures }, new JsonSerializerOptions { WriteIndented = true });
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing stored procedures");
            throw;
        }
    }

    [McpServerTool, Description("Execute a SELECT query (read-only operations only)")]
    public async Task<string> ExecuteQuery(
        [Description("SQL SELECT query to execute")] string query,
        [Description("Maximum number of rows to return (default: 100)")] int maxRows = 100)
    {
        try
        {
            if (string.IsNullOrEmpty(query))
                throw new ArgumentException("query parameter is required");

            var dataTable = await _databaseService.ExecuteQueryAsync(query, maxRows);
            var result = ConvertDataTableToJson(dataTable);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing query: {Query}", query);
            throw;
        }
    }

    private static string ConvertDataTableToJson(System.Data.DataTable dataTable)
    {
        var rows = new List<Dictionary<string, object?>>();

        foreach (System.Data.DataRow row in dataTable.Rows)
        {
            var dict = new Dictionary<string, object?>();
            foreach (System.Data.DataColumn col in dataTable.Columns)
            {
                dict[col.ColumnName] = row[col] == DBNull.Value ? null : row[col];
            }
            rows.Add(dict);
        }

        return JsonSerializer.Serialize(new
        {
            columns = dataTable.Columns.Cast<System.Data.DataColumn>().Select(c => new { name = c.ColumnName, type = c.DataType.Name }),
            rows = rows,
            row_count = rows.Count
        }, new JsonSerializerOptions { WriteIndented = true });
    }
}
