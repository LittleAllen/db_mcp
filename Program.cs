using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using DbMcpServer;
using DbMcpServer.Services;
using ModelContextProtocol.Server;
using System.Reflection;

var builder = Host.CreateApplicationBuilder(args);

// 取得可執行檔案所在的目錄作為基礎路徑
var executableDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;

// 支援自訂設定檔案路徑
var configPath = "appsettings.json";
var configArg = args.FirstOrDefault(arg => arg.StartsWith("--config="));
if (configArg != null)
{
    configPath = configArg.Substring("--config=".Length);
    // 如果是相對路徑，則相對於可執行檔案目錄
    if (!Path.IsPathRooted(configPath))
    {
        configPath = Path.Combine(executableDirectory, configPath);
    }
}
else
{
    // 預設使用可執行檔案目錄下的 appsettings.json
    configPath = Path.Combine(executableDirectory, "appsettings.json");
}

builder.Configuration
    .SetBasePath(executableDirectory)
    .AddJsonFile(configPath, optional: false, reloadOnChange: true)
    .AddEnvironmentVariables()
    .AddCommandLine(args);

builder.Logging.AddConsole(consoleLogOptions =>
{
    // Configure all logs to go to stderr
    consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
});

var connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING") 
    ?? builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string not found. Set CONNECTION_STRING environment variable or DefaultConnection in appsettings.json.");

builder.Services.AddSingleton<DatabaseService>(provider => new DatabaseService(connectionString));
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

await builder.Build().RunAsync();