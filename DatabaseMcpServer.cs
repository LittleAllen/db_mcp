using System.Text.Json;
using ModelContextProtocol;
using DbMcpServer.Services;
using DbMcpServer.Models;
using Microsoft.Extensions.Logging;

namespace DbMcpServer;

public class DatabaseMcpServer : McpServer
{
    private readonly DatabaseService _databaseService;
    private readonly ILogger&lt;DatabaseMcpServer&gt; _logger;

    public DatabaseMcpServer(DatabaseService databaseService, ILogger&lt;DatabaseMcpServer&gt; logger)
    {
        _databaseService = databaseService;
        _logger = logger;
    }

    protected override Task&lt;ServerInfo&gt; GetServerInfoAsync()
    {
        return Task.FromResult(new ServerInfo
        {
            Name = "Database MCP Server",
            Version = "1.0.0"
        });
    }

    protected override Task&lt;ListToolsResult&gt; HandleListToolsAsync()
    {
        var tools = new List&lt;Tool&gt;
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

    protected override async Task&lt;CallToolResult&gt; HandleCallToolAsync(CallToolRequest request)
    {
        try
        {
            _logger.LogInformation("Executing tool: {ToolName}", request.Name);

            return request.Name switch
            {
                "list_tables" =&gt; await HandleListTablesAsync(),
                "get_table_schema" =&gt; await HandleGetTableSchemaAsync(request),
                "get_sample_data" =&gt; await HandleGetSampleDataAsync(request),
                "list_stored_procedures" =&gt; await HandleListStoredProceduresAsync(),
                "execute_query" =&gt; await HandleExecuteQueryAsync(request),
                _ =&gt; new CallToolResult
                {
                    Content = new List&lt;Content&gt;
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
                Content = new List&lt;Content&gt;
                {
                    new TextContent { Text = $"Error: {ex.Message}" }
                },
                IsError = true
            };
        }
    }

    private async Task&lt;CallToolResult&gt; HandleListTablesAsync()
    {
        var tables = await _databaseService.GetTablesAsync();
        var result = JsonSerializer.Serialize(new { tables }, new JsonSerializerOptions { WriteIndented = true });

        return new CallToolResult
        {
            Content = new List&lt;Content&gt;
            {
                new TextContent { Text = result }
            }
        };
    }

    private async Task&lt;CallToolResult&gt; HandleGetTableSchemaAsync(CallToolRequest request)
    {
        var tableName = GetParameterValue&lt;string&gt;(request.Arguments, "table_name");
        if (string.IsNullOrEmpty(tableName))
        {
            return new CallToolResult
            {
                Content = new List&lt;Content&gt; { new TextContent { Text = "table_name parameter is required" } },
                IsError = true
            };
        }

        var schema = await _databaseService.GetTableSchemaAsync(tableName);
        var result = JsonSerializer.Serialize(schema, new JsonSerializerOptions { WriteIndented = true });

        return new CallToolResult
        {
            Content = new List&lt;Content&gt;
            {
                new TextContent { Text = result }
            }
        };
    }

    private async Task&lt;CallToolResult&gt; HandleGetSampleDataAsync(CallToolRequest request)
    {
        var tableName = GetParameterValue&lt;string&gt;(request.Arguments, "table_name");
        var maxRows = GetParameterValue&lt;int?&gt;(request.Arguments, "max_rows") ?? 5;

        if (string.IsNullOrEmpty(tableName))
        {
            return new CallToolResult
            {
                Content = new List&lt;Content&gt; { new TextContent { Text = "table_name parameter is required" } },
                IsError = true
            };
        }

        var dataTable = await _databaseService.GetSampleDataAsync(tableName, maxRows);
        var result = ConvertDataTableToJson(dataTable);

        return new CallToolResult
        {
            Content = new List&lt;Content&gt;
            {
                new TextContent { Text = result }
            }
        };
    }

    private async Task&lt;CallToolResult&gt; HandleListStoredProceduresAsync()
    {
        var procedures = await _databaseService.GetStoredProceduresAsync();
        var result = JsonSerializer.Serialize(new { stored_procedures = procedures }, new JsonSerializerOptions { WriteIndented = true });

        return new CallToolResult
        {
            Content = new List&lt;Content&gt;
            {
                new TextContent { Text = result }
            }
        };
    }

    private async Task&lt;CallToolResult&gt; HandleExecuteQueryAsync(CallToolRequest request)
    {
        var query = GetParameterValue&lt;string&gt;(request.Arguments, "query");
        var maxRows = GetParameterValue&lt;int?&gt;(request.Arguments, "max_rows") ?? 100;

        if (string.IsNullOrEmpty(query))
        {
            return new CallToolResult
            {
                Content = new List&lt;Content&gt; { new TextContent { Text = "query parameter is required" } },
                IsError = true
            };
        }

        var dataTable = await _databaseService.ExecuteQueryAsync(query, maxRows);
        var result = ConvertDataTableToJson(dataTable);

        return new CallToolResult
        {
            Content = new List&lt;Content&gt;
            {
                new TextContent { Text = result }
            }
        };
    }

    private static T? GetParameterValue&lt;T&gt;(JsonDocument? arguments, string parameterName)
    {
        if (arguments?.RootElement.TryGetProperty(parameterName, out var property) == true)
        {
            return JsonSerializer.Deserialize&lt;T&gt;(property.GetRawText());
        }
        return default;
    }

    private static string ConvertDataTableToJson(System.Data.DataTable dataTable)
    {
        var rows = new List&lt;Dictionary&lt;string, object?&gt;&gt;();

        foreach (System.Data.DataRow row in dataTable.Rows)
        {
            var dict = new Dictionary&lt;string, object?&gt;();
            foreach (System.Data.DataColumn col in dataTable.Columns)
            {
                dict[col.ColumnName] = row[col] == DBNull.Value ? null : row[col];
            }
            rows.Add(dict);
        }

        return JsonSerializer.Serialize(new
        {
            columns = dataTable.Columns.Cast&lt;System.Data.DataColumn&gt;().Select(c =&gt; new { name = c.ColumnName, type = c.DataType.Name }),
            rows = rows,
            row_count = rows.Count
        }, new JsonSerializerOptions { WriteIndented = true });
    }
}