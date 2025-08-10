using System.Text.Json;
using ModelContextProtocol;
using DbMcpServer.Services;
using DbMcpServer.Models;
using Microsoft.Extensions.Logging;

namespace DbMcpServer;

public class DatabaseMcpServer : McpServer
{
    private readonly DatabaseService _databaseService;
    private readonly ILogger<DatabaseMcpServer> _logger;

    public DatabaseMcpServer(DatabaseService databaseService, ILogger<DatabaseMcpServer> logger)
    {
        _databaseService = databaseService;
        _logger = logger;
    }

    protected override Task<ServerInfo> GetServerInfoAsync()
    {
        return Task.FromResult(new ServerInfo
        {
            Name = "Database MCP Server",
            Version = "1.0.0"
        });
    }

    protected override Task<ListToolsResult> HandleListToolsAsync()
    {
        var tools = new List<Tool>
        {
            new()
            {
                Name = "list_tables",
                Description = "List all tables in the database",
                InputSchema = JsonSerializer.SerializeToDocument(new { type = "object", properties = new { } })
            },
            new()
            {
                Name = "get_table_schema",
                Description = "Get the schema information for a specific table",
                InputSchema = JsonSerializer.SerializeToDocument(new
                {
                    type = "object",
                    properties = new
                    {
                        table_name = new { type = "string", description = "Name of the table (can include schema: schema.table)" }
                    },
                    required = new[] { "table_name" }
                })
            },
            new()
            {
                Name = "get_sample_data",
                Description = "Get sample data from a table",
                InputSchema = JsonSerializer.SerializeToDocument(new
                {
                    type = "object",
                    properties = new
                    {
                        table_name = new { type = "string", description = "Name of the table" },
                        max_rows = new { type = "integer", description = "Maximum number of rows to return (default: 5)", @default = 5 }
                    },
                    required = new[] { "table_name" }
                })
            },
            new()
            {
                Name = "list_stored_procedures",
                Description = "List all stored procedures in the database",
                InputSchema = JsonSerializer.SerializeToDocument(new { type = "object", properties = new { } })
            },
            new()
            {
                Name = "execute_query",
                Description = "Execute a SELECT query (read-only operations only)",
                InputSchema = JsonSerializer.SerializeToDocument(new
                {
                    type = "object",
                    properties = new
                    {
                        query = new { type = "string", description = "SQL SELECT query to execute" },
                        max_rows = new { type = "integer", description = "Maximum number of rows to return (default: 100)", @default = 100 }
                    },
                    required = new[] { "query" }
                })
            }
        };

        return Task.FromResult(new ListToolsResult { Tools = tools });
    }

    protected override async Task<CallToolResult> HandleCallToolAsync(CallToolRequest request)
    {
        try
        {
            _logger.LogInformation("Executing tool: {ToolName}", request.Name);

            return request.Name switch
            {
                "list_tables" => await HandleListTablesAsync(),
                "get_table_schema" => await HandleGetTableSchemaAsync(request),
                "get_sample_data" => await HandleGetSampleDataAsync(request),
                "list_stored_procedures" => await HandleListStoredProceduresAsync(),
                "execute_query" => await HandleExecuteQueryAsync(request),
                _ => new CallToolResult
                {
                    Content = new List<Content>
                    {
                        new TextContent { Text = $"Unknown tool: {request.Name}" }
                    },
                    IsError = true
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing tool {ToolName}", request.Name);
            return new CallToolResult
            {
                Content = new List<Content>
                {
                    new TextContent { Text = $"Error: {ex.Message}" }
                },
                IsError = true
            };
        }
    }

    private async Task<CallToolResult> HandleListTablesAsync()
    {
        var tables = await _databaseService.GetTablesAsync();
        var result = JsonSerializer.Serialize(new { tables }, new JsonSerializerOptions { WriteIndented = true });

        return new CallToolResult
        {
            Content = new List<Content>
            {
                new TextContent { Text = result }
            }
        };
    }

    private async Task<CallToolResult> HandleGetTableSchemaAsync(CallToolRequest request)
    {
        var tableName = GetParameterValue<string>(request.Arguments, "table_name");
        if (string.IsNullOrEmpty(tableName))
        {
            return new CallToolResult
            {
                Content = new List<Content> { new TextContent { Text = "table_name parameter is required" } },
                IsError = true
            };
        }

        var schema = await _databaseService.GetTableSchemaAsync(tableName);
        var result = JsonSerializer.Serialize(schema, new JsonSerializerOptions { WriteIndented = true });

        return new CallToolResult
        {
            Content = new List<Content>
            {
                new TextContent { Text = result }
            }
        };
    }

    private async Task<CallToolResult> HandleGetSampleDataAsync(CallToolRequest request)
    {
        var tableName = GetParameterValue<string>(request.Arguments, "table_name");
        var maxRows = GetParameterValue<int?>(request.Arguments, "max_rows") ?? 5;

        if (string.IsNullOrEmpty(tableName))
        {
            return new CallToolResult
            {
                Content = new List<Content> { new TextContent { Text = "table_name parameter is required" } },
                IsError = true
            };
        }

        var dataTable = await _databaseService.GetSampleDataAsync(tableName, maxRows);
        var result = ConvertDataTableToJson(dataTable);

        return new CallToolResult
        {
            Content = new List<Content>
            {
                new TextContent { Text = result }
            }
        };
    }

    private async Task<CallToolResult> HandleListStoredProceduresAsync()
    {
        var procedures = await _databaseService.GetStoredProceduresAsync();
        var result = JsonSerializer.Serialize(new { stored_procedures = procedures }, new JsonSerializerOptions { WriteIndented = true });

        return new CallToolResult
        {
            Content = new List<Content>
            {
                new TextContent { Text = result }
            }
        };
    }

    private async Task<CallToolResult> HandleExecuteQueryAsync(CallToolRequest request)
    {
        var query = GetParameterValue<string>(request.Arguments, "query");
        var maxRows = GetParameterValue<int?>(request.Arguments, "max_rows") ?? 100;

        if (string.IsNullOrEmpty(query))
        {
            return new CallToolResult
            {
                Content = new List<Content> { new TextContent { Text = "query parameter is required" } },
                IsError = true
            };
        }

        var dataTable = await _databaseService.ExecuteQueryAsync(query, maxRows);
        var result = ConvertDataTableToJson(dataTable);

        return new CallToolResult
        {
            Content = new List<Content>
            {
                new TextContent { Text = result }
            }
        };
    }

    private static T? GetParameterValue<T>(JsonDocument? arguments, string parameterName)
    {
        if (arguments?.RootElement.TryGetProperty(parameterName, out var property) == true)
        {
            return JsonSerializer.Deserialize<T>(property.GetRawText());
        }
        return default;
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
