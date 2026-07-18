using System.Text.Json;
using Microsoft.Extensions.Logging;
using SevenPace.McpServer.Configuration;
using SevenPace.McpServer.Dtos;

namespace SevenPace.McpServer.Services;

/// <summary>
/// Business logic for 7Pace worklog operations.
/// Handles validation, limited mode, and delegates HTTP calls to SevenPaceClient.
/// </summary>
public sealed class WorklogService
{
    private readonly SevenPaceClient _client;
    private readonly SevenPaceOptions _options;
    private readonly ILogger<WorklogService>? _logger;

    public WorklogService(SevenPaceClient client, SevenPaceOptions options, ILogger<WorklogService>? logger = null)
    {
        _client = client;
        _options = options;
        _logger = logger;
    }

    /// <summary>
    /// Creates a worklog entry. Returns the API response as OperationResult.
    /// In limited mode, returns an informational message without calling the API.
    /// Accepts DateTimeOffset for maximum MCP client compatibility.
    /// </summary>
    public async Task<OperationResult> LogTimeAsync(
        int workItemId,
        DateTimeOffset date,
        double hours,
        string description,
        string? activityType = null,
        CancellationToken cancellationToken = default)
    {
        if (_options.IsLimitedMode)
        {
            return OperationResult.Ok(new
            {
                limitedMode = true,
                message = "7pace MCP server is reachable (limited mode). Provide SEVENPACE_ORGANIZATION and SEVENPACE_TOKEN to enable time logging."
            });
        }

        // Validate inputs
        if (workItemId <= 0)
            return OperationResult.Fail("workItemId must be a positive integer");
        if (hours <= 0 || !double.IsFinite(hours))
            return OperationResult.Fail("hours must be a positive number");
        if (string.IsNullOrWhiteSpace(description))
            return OperationResult.Fail("description is required");

        try
        {
            var request = new CreateWorklogRequest
            {
                WorkItemId = workItemId,
                TimeStamp = date.ToString("yyyy-MM-ddTHH:mm:sszzz"),
                Length = (int)Math.Round(hours * 3600),
                Comment = description,
                ActivityTypeId = activityType
            };

            var response = await _client.CreateWorklogAsync(request, cancellationToken);

            return OperationResult.Ok(new
            {
                success = true,
                message = $"Time logged successfully! Work Item: #{workItemId}, Date: {date:O}, Hours: {hours}, Description: {description}",
                worklogId = ExtractWorklogId(response)
            });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "LogTime failed for work item {WorkItemId}", workItemId);
            return OperationResult.Fail($"Failed to log time: {ex.Message}");
        }
    }

    /// <summary>
    /// Retrieves worklogs with optional filters.
    /// Accepts DateTimeOffset for maximum MCP client compatibility.
    /// The API filters are exclusive (>, <), so we subtract 1 second from
    /// the start and add end-of-day to the end to make the range inclusive.
    /// </summary>
    public async Task<OperationResult> GetWorklogsAsync(
        int? workItemId = null,
        DateTimeOffset? startDate = null,
        DateTimeOffset? endDate = null,
        DateTimeOffset? createdStartDate = null,
        DateTimeOffset? createdEndDate = null,
        CancellationToken cancellationToken = default)
    {
        if (_options.IsLimitedMode)
        {
            return OperationResult.Ok(new
            {
                limitedMode = true,
                message = "Worklogs unavailable in limited mode. Provide SEVENPACE_ORGANIZATION and SEVENPACE_TOKEN."
            });
        }

        // Validate date ranges if provided
        if (startDate is not null && endDate is not null && startDate > endDate)
            return OperationResult.Fail("startDate must be before or equal to endDate");
        if (createdStartDate is not null && createdEndDate is not null && createdStartDate > createdEndDate)
            return OperationResult.Fail("createdStartDate must be before or equal to createdEndDate");

        try
        {
            // API uses exclusive filters (>, <), so we subtract 1 second from
            // the start and use end-of-day for the end to make the range inclusive.
            string? fromTimestamp = startDate is not null
                ? startDate.Value.AddSeconds(-1).ToString("yyyy-MM-ddTHH:mm:sszzz")
                : null;
            string? toTimestamp = endDate is not null
                ? endDate.Value.Date.AddDays(1).AddSeconds(-1).ToString("yyyy-MM-ddTHH:mm:sszzz")
                : null;
            string? fromCreatedTimestamp = createdStartDate is not null
                ? createdStartDate.Value.AddSeconds(-1).ToString("yyyy-MM-ddTHH:mm:sszzz")
                : null;
            string? toCreatedTimestamp = createdEndDate is not null
                ? createdEndDate.Value.Date.AddDays(1).AddSeconds(-1).ToString("yyyy-MM-ddTHH:mm:sszzz")
                : null;

            var worklogs = await _client.GetWorklogsAsync(
                workItemId, fromTimestamp, toTimestamp,
                fromCreatedTimestamp, toCreatedTimestamp,
                cancellationToken);

            var summary = worklogs.Select(w => new
            {
                id = w.Id ?? "N/A",
                workItemId = w.WorkItemId,
                timestamp = w.Timestamp,
                hours = w.Length.HasValue ? ComputeHoursFromApiLength(w.Length.Value) : (double?)null,
                comment = w.Comment ?? "No description"
            }).ToList();

            return OperationResult.Ok(new
            {
                count = worklogs.Count,
                worklogs = summary
            });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "GetWorklogs failed");
            return OperationResult.Fail($"Failed to retrieve worklogs: {ex.Message}");
        }
    }

    /// <summary>
    /// Updates an existing worklog. Only provided fields are updated.
    /// </summary>
    public async Task<OperationResult> UpdateWorklogAsync(
        string worklogId,
        int? workItemId = null,
        double? hours = null,
        string? description = null,
        CancellationToken cancellationToken = default)
    {
        if (_options.IsLimitedMode)
        {
            return OperationResult.Ok(new
            {
                limitedMode = true,
                message = "Server reachable (limited mode). Provide SEVENPACE_* to enable update_worklog."
            });
        }

        if (string.IsNullOrWhiteSpace(worklogId))
            return OperationResult.Fail("worklogId is required");

        if (workItemId is null && hours is null && string.IsNullOrWhiteSpace(description))
            return OperationResult.Fail("Provide at least one field to update: workItemId, hours, or description");

        if (workItemId is <= 0)
            return OperationResult.Fail("workItemId must be a positive integer");

        if (hours is <= 0 || (hours is not null && !double.IsFinite(hours.Value)))
            return OperationResult.Fail("hours must be a positive number");

        try
        {
            var request = new UpdateWorklogRequest
            {
                Length = hours.HasValue ? (int)Math.Round(hours.Value * 3600) : null,
                Comment = string.IsNullOrWhiteSpace(description) ? null : description,
                WorkItemId = workItemId
            };

            await _client.UpdateWorklogAsync(worklogId, request, cancellationToken);

            return OperationResult.Ok(new
            {
                success = true,
                message = $"Worklog {worklogId} updated successfully!"
            });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "UpdateWorklog failed for {WorklogId}", worklogId);
            return OperationResult.Fail($"Failed to update worklog: {ex.Message}");
        }
    }

    /// <summary>
    /// Deletes a worklog by ID.
    /// </summary>
    public async Task<OperationResult> DeleteWorklogAsync(
        string worklogId,
        CancellationToken cancellationToken = default)
    {
        if (_options.IsLimitedMode)
        {
            return OperationResult.Ok(new
            {
                limitedMode = true,
                message = "Server reachable (limited mode). Provide SEVENPACE_* to enable delete_worklog."
            });
        }

        if (string.IsNullOrWhiteSpace(worklogId))
            return OperationResult.Fail("worklogId is required");

        try
        {
            await _client.DeleteWorklogAsync(worklogId, cancellationToken);

            return OperationResult.Ok(new
            {
                success = true,
                message = $"Worklog {worklogId} deleted successfully!"
            });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "DeleteWorklog failed for {WorklogId}", worklogId);
            return OperationResult.Fail($"Failed to delete worklog: {ex.Message}");
        }
    }

    /// <summary>
    /// Heuristic: REST list often returns seconds; create/update uses seconds.
    /// If the number is large (>= 1000), assume seconds; else minutes.
    /// Matches the Node.js computeHoursFromApiLength behavior.
    /// </summary>
    internal static double ComputeHoursFromApiLength(int length) =>
        length >= 1000 ? length / 3600.0 : length / 60.0;

    private static string? ExtractWorklogId(JsonDocument response)
    {
        if (response.RootElement.TryGetProperty("id", out var idProp))
            return idProp.GetString();
        if (response.RootElement.TryGetProperty("Id", out var idProp2))
            return idProp2.GetString();
        return null;
    }
}