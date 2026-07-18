using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SevenPace.McpServer;
using SevenPace.McpServer.Configuration;
using SevenPace.McpServer.Services;

// Determine transport mode from the PORT environment variable.
// When PORT is set to a valid integer, the server runs in HTTP (Streamable HTTP,
// stateless) mode; otherwise it defaults to stdio — matching the Node reference
// implementation's transport selection logic.
var portEnv = Environment.GetEnvironmentVariable("PORT");
var useHttp = !string.IsNullOrWhiteSpace(portEnv) && int.TryParse(portEnv, out _);

if (useHttp)
{
    // ── HTTP mode: Streamable HTTP stateless transport ──────────────────────
    var builder = WebApplication.CreateBuilder(args);

    // Logging to stderr only — keeps the HTTP response stream clean for MCP.
    builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);

    // Configure SevenPaceOptions from environment variables (manual binding,
    // matching the pattern used in the mcp-devops reference server).
    var options = new SevenPaceOptions
    {
        Organization = Environment.GetEnvironmentVariable("SEVENPACE_ORGANIZATION") ?? string.Empty,
        Token = Environment.GetEnvironmentVariable("SEVENPACE_TOKEN"),
        BaseUrl = Environment.GetEnvironmentVariable("SEVENPACE_BASE_URL"),
        WriteTimeoutMs = int.TryParse(Environment.GetEnvironmentVariable("SEVENPACE_WRITE_TIMEOUT_MS"), out var httpWtm) ? httpWtm : 30000,
        TimeZone = Environment.GetEnvironmentVariable("TZ")
    };

    // Register services
    builder.Services.AddSingleton(options);
    builder.Services.AddHttpClient<SevenPaceClient>();
    builder.Services.AddSingleton<WorklogService>();
    builder.Services.AddSingleton<ActivityTypeService>();
    builder.Services.AddSingleton<ReportService>();

    // MCP server with HTTP transport (stateless, per Smithery container requirements)
    builder.Services.AddMcpServer()
        .WithHttpTransport(o => o.Stateless = true)
        .WithToolsFromAssembly();

    var app = builder.Build();
    app.MapMcp("/mcp");
    app.Run();
}
else
{
    // ── stdio mode: default transport (MCP JSON-RPC over stdin/stdout) ──────
    var builder = Host.CreateApplicationBuilder(args);

    // Logging to stderr only — stdout is reserved for MCP JSON-RPC frames.
    builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);

    // Configure SevenPaceOptions from environment variables (manual binding,
    // matching the pattern used in the mcp-devops reference server).
    var options = new SevenPaceOptions
    {
        Organization = Environment.GetEnvironmentVariable("SEVENPACE_ORGANIZATION") ?? string.Empty,
        Token = Environment.GetEnvironmentVariable("SEVENPACE_TOKEN"),
        BaseUrl = Environment.GetEnvironmentVariable("SEVENPACE_BASE_URL"),
        WriteTimeoutMs = int.TryParse(Environment.GetEnvironmentVariable("SEVENPACE_WRITE_TIMEOUT_MS"), out var stdioWtm) ? stdioWtm : 30000,
        TimeZone = Environment.GetEnvironmentVariable("TZ")
    };

    // Register services
    builder.Services.AddSingleton(options);
    builder.Services.AddHttpClient<SevenPaceClient>();
    builder.Services.AddSingleton<WorklogService>();
    builder.Services.AddSingleton<ActivityTypeService>();
    builder.Services.AddSingleton<ReportService>();

    // MCP server with stdio transport. WithToolsFromAssembly discovers the
    // [McpServerToolType] Tools class and its [McpServerTool] partial methods.
    builder.Services.AddMcpServer()
        .WithStdioServerTransport()
        .WithToolsFromAssembly();

    var host = builder.Build();

    using var cts = new CancellationTokenSource();
    Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

    try
    {
        await host.RunAsync(cts.Token);
    }
    catch (Exception ex)
    {
        if (host.Services.GetService(typeof(ILogger<Program>)) is ILogger<Program> logger)
        {
            logger.LogCritical(ex, "Unhandled exception during host execution");
        }
        else
        {
            Console.Error.WriteLine($"Unhandled exception: {ex}");
        }
        Environment.ExitCode = 1;
    }
}