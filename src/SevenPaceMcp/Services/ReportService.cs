using System.Text.Json;
using Microsoft.Extensions.Logging;
using SevenPace.McpServer.Configuration;
using SevenPace.McpServer.Dtos;

namespace SevenPace.McpServer.Services;

/// <summary>
/// Service for generating 7Pace time reports.
/// Falls back to aggregating from worklogs if the report API fails.
/// </summary>
public sealed class ReportService
{
    private readonly SevenPaceClient _client;
    private readonly SevenPaceOptions _options;
    private readonly ILogger<ReportService>? _logger;

    public ReportService(SevenPaceClient client, SevenPaceOptions options, ILogger<ReportService>? logger = null)
    {
        _client = client;
        _options = options;
        _logger = logger;
    }

    /// <summary>
    /// Generates a time report for a date range.
    /// If the report API fails, falls back to computing totals from worklogs.
    /// Accepts DateTimeOffset for maximum MCP client compatibility.
    /// </summary>
    public async Task<OperationResult> GenerateTimeReportAsync(
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        string? userId = null,
        CancellationToken cancellationToken = default)
    {
        if (_options.IsLimitedMode)
        {
            return OperationResult.Ok(new
            {
                limitedMode = true,
                message = "Reports unavailable in limited mode. Provide SEVENPACE_* to enable."
            });
        }

        // Validate date range
        if (startDate > endDate)
            return OperationResult.Fail("startDate must be before or equal to endDate");

        var startDateStr = startDate.ToString("O");
        var endDateStr = endDate.ToString("O");

        // Try the report API first
        try
        {
            var report = await _client.GetTimeReportAsync(startDateStr, endDateStr, userId, cancellationToken);
            var root = report.RootElement;

            double? totalHours = null;
            int? totalEntries = null;

            if (root.TryGetProperty("totalHours", out var hoursProp) && hoursProp.ValueKind == JsonValueKind.Number)
                totalHours = hoursProp.GetDouble();
            if (root.TryGetProperty("totalEntries", out var entriesProp) && entriesProp.ValueKind == JsonValueKind.Number)
                totalEntries = entriesProp.GetInt32();

            return OperationResult.Ok(new
            {
                source = "api",
                startDate = startDateStr,
                endDate = endDateStr,
                totalHours,
                totalEntries,
                rawData = root
            });
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Report API failed, falling back to worklog aggregation");
        }

        // Fallback: compute from worklogs
        try
        {
            var fromTimestamp = startDate.AddSeconds(-1).ToString("yyyy-MM-ddTHH:mm:sszzz");
            var toTimestamp = endDate.Date.AddDays(1).AddSeconds(-1).ToString("yyyy-MM-ddTHH:mm:sszzz");

            var worklogs = await _client.GetWorklogsAsync(
                null, fromTimestamp, toTimestamp, null, null, cancellationToken);

            double totalHoursComputed = 0;
            var totalEntriesComputed = worklogs.Count;

            foreach (var wl in worklogs)
            {
                if (wl.Length.HasValue)
                {
                    totalHoursComputed += ComputeHoursFromApiLength(wl.Length.Value);
                }
            }

            return OperationResult.Ok(new
            {
                source = "computed",
                startDate = startDateStr,
                endDate = endDateStr,
                totalHours = Math.Round(totalHoursComputed, 2),
                totalEntries = totalEntriesComputed
            });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Report fallback also failed");
            return OperationResult.Fail($"Failed to generate time report: {ex.Message}");
        }
    }

    /// <summary>
    /// Heuristic matching Node.js computeHoursFromApiLength:
    /// If length >= 1000, assume seconds; else minutes.
    /// </summary>
    private static double ComputeHoursFromApiLength(int length) =>
        length >= 1000 ? length / 3600.0 : length / 60.0;
}