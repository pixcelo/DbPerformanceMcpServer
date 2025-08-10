using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using DbPerformanceMcpServer.Tools;
using DbPerformanceMcpServer.Services;
using DbPerformanceMcpServer.Configuration;

var builder = Host.CreateApplicationBuilder(args);

// Configure all logs to go to stderr (stdout is used for the MCP protocol messages).
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);

// Add configuration from appsettings.json (look in exe directory first, then fallback to current directory)
var exeDirectory = AppContext.BaseDirectory;
var settingsPath = Path.Combine(exeDirectory, "appsettings.json");

if (File.Exists(settingsPath))
{
    builder.Configuration.AddJsonFile(settingsPath, optional: false, reloadOnChange: false);
}
else
{
    builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);
}

// Configure options
builder.Services.Configure<DbOptimizerOptions>(
    builder.Configuration.GetSection("DbPerformanceOptimizer"));

// Register core services for dependency injection
builder.Services.AddSingleton<ISqlConnectionService, SqlConnectionService>();
builder.Services.AddSingleton<IViewAnalysisService, ViewAnalysisService>();
builder.Services.AddSingleton<IOptimizationService, OptimizationService>();
builder.Services.AddSingleton<IValidationService, ValidationService>();
builder.Services.AddSingleton<ISnapshotService, SnapshotService>();
builder.Services.AddSingleton<IExecutionPlanAnalyzer, ExecutionPlanAnalyzer>();
builder.Services.AddSingleton<IOptimizationOrchestrator, OptimizationOrchestrator>();

// Add the MCP services: the transport to use (stdio) and the tools to register.
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<DbPerformanceMcpTools>();

await builder.Build().RunAsync();