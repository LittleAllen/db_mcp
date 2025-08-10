using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using DbMcpServer;
using DbMcpServer.Services;
using ModelContextProtocol.Extensions;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables();

builder.Services.AddLogging(configure =&gt; configure.AddConsole());

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddSingleton&lt;DatabaseService&gt;(provider =&gt; new DatabaseService(connectionString));
builder.Services.AddSingleton&lt;DatabaseMcpServer&gt;();

builder.Services.AddMcp();

var host = builder.Build();

var logger = host.Services.GetRequiredService&lt;ILogger&lt;Program&gt;&gt;();
var mcpServer = host.Services.GetRequiredService&lt;DatabaseMcpServer&gt;();

logger.LogInformation("Starting Database MCP Server...");
logger.LogInformation("Connection String: {ConnectionString}", connectionString.Replace(";Password=", ";Password=***"));

try
{
    await mcpServer.RunAsync(Console.OpenStandardInput(), Console.OpenStandardOutput());
}
catch (Exception ex)
{
    logger.LogError(ex, "Error running MCP server");
    Environment.Exit(1);
}