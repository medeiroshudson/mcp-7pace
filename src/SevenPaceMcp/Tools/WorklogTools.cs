using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace SevenPace.McpServer;

/// <summary>
/// Worklog-related MCP tools: log_time, get_worklogs, update_worklog, delete_worklog.
/// Each tool delegates business logic to <see cref="Services.WorklogService"/> and
/// returns a JSON-serialized <see cref="OperationResult"/> string.
/// </summary>
public sealed partial class Tools
{
    [McpServerTool(
        Name = "log_time",
        Title = "Log Time",
        ReadOnly = false,
        Idempotent = false,
        Destructive = false),
        Description("Log time entry to 7pace Timetracker for a specific work item")]
    public async Task<string> LogTime(
        [Description("Azure DevOps Work Item ID")] int workItemId,
        [Description("Date the work was performed (ISO 8601, e.g. 2026-07-17 or 2026-07-17T14:30:00)")] DateTimeOffset date,
        [Description("Number of hours worked")] double hours,
        [Description("Description of work performed")] string description,
        [Description("Type of activity (name or ID, optional)")] string? activityType = null,
        CancellationToken cancellationToken = default)
    {
        // Resolve activity type name to its GUID id before delegating to the service.
        // Invalid/unknown names are silently omitted (matches Node reference behavior).
        string? activityTypeId = null;
        if (!string.IsNullOrWhiteSpace(activityType))
        {
            activityTypeId = await _activityTypeService.ResolveActivityTypeIdAsync(activityType, cancellationToken);
        }

        var result = await _worklogService.LogTimeAsync(workItemId, date, hours, description, activityTypeId, cancellationToken);
        return SerializeResult(result);
    }

    [McpServerTool(
        Name = "get_worklogs",
        Title = "Get Worklogs",
        ReadOnly = true,
        Idempotent = true,
        Destructive = false),
        Description("Retrieve time logs from 7pace Timetracker")]
    public async Task<string> GetWorklogs(
        [Description("Filter by specific work item ID (optional)")] int? workItemId = null,
        [Description("Start date — filters by worklog timestamp (optional, ISO 8601)")] DateTimeOffset? startDate = null,
        [Description("End date — filters by worklog timestamp (optional, ISO 8601)")] DateTimeOffset? endDate = null,
        [Description("Start date — filters by worklog creation date (optional, ISO 8601)")] DateTimeOffset? createdStartDate = null,
        [Description("End date — filters by worklog creation date (optional, ISO 8601)")] DateTimeOffset? createdEndDate = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _worklogService.GetWorklogsAsync(workItemId, startDate, endDate, createdStartDate, createdEndDate, cancellationToken);
        return SerializeResult(result);
    }

    [McpServerTool(
        Name = "update_worklog",
        Title = "Update Worklog",
        ReadOnly = false,
        Idempotent = true,
        Destructive = false),
        Description("Update an existing time log entry")]
    public async Task<string> UpdateWorklog(
        [Description("ID of the worklog to update")] string worklogId,
        [Description("New work item ID (optional)")] int? workItemId = null,
        [Description("New number of hours (optional)")] double? hours = null,
        [Description("New description (optional)")] string? description = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _worklogService.UpdateWorklogAsync(worklogId, workItemId, hours, description, cancellationToken);
        return SerializeResult(result);
    }

    [McpServerTool(
        Name = "delete_worklog",
        Title = "Delete Worklog",
        ReadOnly = false,
        Idempotent = false,
        Destructive = true),
        Description("Delete a time log entry")]
    public async Task<string> DeleteWorklog(
        [Description("ID of the worklog to delete")] string worklogId,
        CancellationToken cancellationToken = default)
    {
        var result = await _worklogService.DeleteWorklogAsync(worklogId, cancellationToken);
        return SerializeResult(result);
    }

    /// <summary>
    /// Serializes an <see cref="OperationResult"/> to a stable JSON shape
    /// (<c>{ success, error, data }</c>) suitable for MCP tool responses.
    /// </summary>
    private static string SerializeResult(OperationResult result)
    {
        return JsonSerializer.Serialize(new
        {
            success = result.Success,
            error = result.Error,
            data = result.Data
        }, new JsonSerializerOptions { WriteIndented = true });
    }
}