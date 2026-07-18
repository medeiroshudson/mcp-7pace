using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using SevenPace.McpServer.Configuration;

namespace SevenPace.McpServer;

/// <summary>
/// System-level MCP tools: health (deployment scanner probe) and
/// configure_sevenpace (runtime credential validation + guidance).
/// </summary>
public sealed partial class Tools
{
    [McpServerTool(
        Name = "health",
        Title = "Health Check",
        ReadOnly = true,
        Idempotent = true,
        Destructive = false),
        Description("Simple health check for deployment scanners")]
    public string Health()
    {
        var limited = _options.IsLimitedMode;
        return JsonSerializer.Serialize(new
        {
            status = "healthy",
            limitedMode = limited,
            message = limited
                ? "7pace MCP server is healthy (limited mode). Set SEVENPACE_ORGANIZATION and SEVENPACE_TOKEN to enable full functionality."
                : "7pace MCP server is healthy and reachable"
        }, new JsonSerializerOptions { WriteIndented = true });
    }

    [McpServerTool(
        Name = "configure_sevenpace",
        Title = "Configure 7Pace",
        ReadOnly = false,
        Idempotent = true,
        Destructive = false),
        Description("Configure 7pace credentials at runtime (organization and token)")]
    public string ConfigureSevenPace(
        [Description("7pace organization (e.g., labournet)")] string organization,
        [Description("7pace API token")] string token,
        [Description("Optional override for 7pace base URL (advanced)")] string? baseUrl = null)
    {
        if (string.IsNullOrWhiteSpace(organization))
            return SerializeResult(OperationResult.Fail("organization is required and must be a non-empty string"));
        if (string.IsNullOrWhiteSpace(token))
            return SerializeResult(OperationResult.Fail("token is required and must be a non-empty string"));

        // Note: In this .NET implementation, runtime configuration changes require
        // restarting the server with new environment variables. The Node.js version
        // could mutate process.env and recreate the service, but the typed HttpClient
        // pattern in .NET makes this impractical at runtime.
        //
        // This tool validates the inputs and returns guidance.
        var resolvedBaseUrl = !string.IsNullOrWhiteSpace(baseUrl)
            ? baseUrl!
            : $"https://{organization.Trim()}.timehub.7pace.com";

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = $"Configuration received for org '{organization.Trim()}'. To apply, restart the server with SEVENPACE_ORGANIZATION={organization.Trim()} and SEVENPACE_TOKEN set.",
            baseUrl = resolvedBaseUrl
        }, new JsonSerializerOptions { WriteIndented = true });
    }
}