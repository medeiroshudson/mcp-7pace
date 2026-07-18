using System.ComponentModel;
using ModelContextProtocol.Server;

namespace SevenPace.McpServer;

/// <summary>
/// Activity-type and report MCP tools: list_activity_types and generate_time_report.
/// Each tool delegates business logic to the corresponding service and returns a
/// JSON-serialized <see cref="OperationResult"/> string via <see cref="SerializeResult"/>.
/// </summary>
public sealed partial class Tools
{
    [McpServerTool(
        Name = "list_activity_types",
        Title = "List Activity Types",
        ReadOnly = true,
        Idempotent = true,
        Destructive = false),
        Description("List available 7pace activity types (name and id)")]
    public async Task<string> ListActivityTypes(CancellationToken cancellationToken = default)
    {
        var result = await _activityTypeService.GetActivityTypesAsync(cancellationToken);
        return SerializeResult(result);
    }

    [McpServerTool(
        Name = "generate_time_report",
        Title = "Generate Time Report",
        ReadOnly = true,
        Idempotent = true,
        Destructive = false),
        Description("Generate time tracking report for a date range")]
    public async Task<string> GenerateTimeReport(
        [Description("Start date of the report range (ISO 8601, e.g. 2026-07-01 or 2026-07-01T00:00:00)")] DateTimeOffset startDate,
        [Description("End date of the report range (ISO 8601, e.g. 2026-07-17 or 2026-07-17T23:59:59)")] DateTimeOffset endDate,
        [Description("Filter by specific user ID (optional)")] string? userId = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _reportService.GenerateTimeReportAsync(startDate, endDate, userId, cancellationToken);
        return SerializeResult(result);
    }
}