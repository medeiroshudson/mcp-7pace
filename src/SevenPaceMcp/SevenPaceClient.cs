using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SevenPace.McpServer.Configuration;
using SevenPace.McpServer.Dtos;

namespace SevenPace.McpServer;

/// <summary>
/// Typed HttpClient for 7Pace Timetracker REST API v3.2.
/// Handles authentication, serialization, and error mapping.
/// </summary>
public sealed class SevenPaceClient
{
    private readonly HttpClient _httpClient;
    private readonly SevenPaceOptions _options;
    private readonly ILogger<SevenPaceClient>? _logger;

    private const string ApiVersion = "3.2";

    public SevenPaceClient(HttpClient httpClient, SevenPaceOptions options, ILogger<SevenPaceClient>? logger = null)
    {
        _httpClient = httpClient;
        _options = options;
        _logger = logger;

        var baseUrl = options.ResolveBaseUrl();
        if (!baseUrl.EndsWith("/"))
            baseUrl += "/";

        _httpClient.BaseAddress = new Uri(baseUrl);

        if (!string.IsNullOrWhiteSpace(options.Token))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", options.Token);
        }

        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
    }

    // Activity Types
    public async Task<ActivityType[]> GetActivityTypesAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(
            $"api/rest/activitytypes?api-version={ApiVersion}",
            cancellationToken);

        await EnsureSuccessStatusCodeAsync(response, cancellationToken);

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var apiResponse = JsonSerializer.Deserialize(json, SevenPaceJsonContext.Default.ActivityTypeResponse);

        return apiResponse?.Data?.ActivityTypes?.ToArray() ?? [];
    }

    // Create Worklog
    public async Task<JsonDocument> CreateWorklogAsync(
        CreateWorklogRequest request,
        CancellationToken cancellationToken = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromMilliseconds(_options.WriteTimeoutMs));

        var content = JsonContent.Create(request, SevenPaceJsonContext.Default.CreateWorklogRequest);
        var response = await _httpClient.PostAsync(
            $"api/rest/workLogs?api-version={ApiVersion}",
            content,
            cts.Token);

        await EnsureSuccessStatusCodeAsync(response, cts.Token);

        var json = await response.Content.ReadAsStringAsync(cts.Token);
        return JsonDocument.Parse(json);
    }

    // Get Worklogs
    /// <summary>
    /// Retrieves worklogs from the 7Pace API with pagination and filtering.
    /// Uses the official v3.2 parameters: $workItemIds, $fromTimestamp, $toTimestamp,
    /// $fromCreatedTimestamp, $toCreatedTimestamp, $count (page size, max 500), $skip (offset).
    /// Automatically paginates through all results.
    /// </summary>
    public async Task<List<WorklogItem>> GetWorklogsAsync(
        int? workItemId = null,
        string? fromTimestamp = null,
        string? toTimestamp = null,
        string? fromCreatedTimestamp = null,
        string? toCreatedTimestamp = null,
        CancellationToken cancellationToken = default)
    {
        const int pageSize = 500;
        var allItems = new List<WorklogItem>();
        var skip = 0;

        // Loop until a page returns fewer than pageSize items (last page)
        while (true)
        {
            var query = BuildWorklogsQuery(
                workItemId, fromTimestamp, toTimestamp,
                fromCreatedTimestamp, toCreatedTimestamp,
                pageSize, skip);

            var response = await _httpClient.GetAsync(query, cancellationToken);
            await EnsureSuccessStatusCodeAsync(response, cancellationToken);

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var pageItems = ParseWorklogsResponse(json);

            allItems.AddRange(pageItems);

            if (pageItems.Count < pageSize)
                break;

            skip += pageSize;
        }

        return allItems;
    }

    private string BuildWorklogsQuery(
        int? workItemId,
        string? fromTimestamp,
        string? toTimestamp,
        string? fromCreatedTimestamp,
        string? toCreatedTimestamp,
        int count,
        int skip)
    {
        var query = $"api/rest/workLogs?api-version={ApiVersion}";

        if (workItemId.HasValue)
            query += $"&$workItemIds={workItemId.Value}";

        if (!string.IsNullOrWhiteSpace(fromTimestamp))
            query += $"&$fromTimestamp={Uri.EscapeDataString(fromTimestamp)}";

        if (!string.IsNullOrWhiteSpace(toTimestamp))
            query += $"&$toTimestamp={Uri.EscapeDataString(toTimestamp)}";

        if (!string.IsNullOrWhiteSpace(fromCreatedTimestamp))
            query += $"&$fromCreatedTimestamp={Uri.EscapeDataString(fromCreatedTimestamp)}";

        if (!string.IsNullOrWhiteSpace(toCreatedTimestamp))
            query += $"&$toCreatedTimestamp={Uri.EscapeDataString(toCreatedTimestamp)}";

        query += $"&$count={count}";
        query += $"&$skip={skip}";

        return query;
    }

    private static List<WorklogItem> ParseWorklogsResponse(string json)
    {
        // The API returns { data: [...] } per the v3.2 spec.
        // We also handle { value: [...] } (OData) and bare array as fallbacks.
        var listResponse = JsonSerializer.Deserialize(json, SevenPaceJsonContext.Default.WorklogListResponse);

        if (listResponse?.Data is not null)
            return listResponse.Data;
        if (listResponse?.Value is not null)
            return listResponse.Value;

        // Try bare array
        var items = JsonSerializer.Deserialize(json, SevenPaceJsonContext.Default.ListWorklogItem);
        return items ?? [];
    }

    // Update Worklog
    public async Task<JsonDocument> UpdateWorklogAsync(
        string worklogId,
        UpdateWorklogRequest request,
        CancellationToken cancellationToken = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromMilliseconds(_options.WriteTimeoutMs));

        var content = JsonContent.Create(request, SevenPaceJsonContext.Default.UpdateWorklogRequest);
        var response = await _httpClient.PutAsync(
            $"api/rest/worklogs/{Uri.EscapeDataString(worklogId)}?api-version={ApiVersion}",
            content,
            cts.Token);

        await EnsureSuccessStatusCodeAsync(response, cts.Token);

        var json = await response.Content.ReadAsStringAsync(cts.Token);
        return JsonDocument.Parse(json);
    }

    // Delete Worklog
    public async Task DeleteWorklogAsync(
        string worklogId,
        CancellationToken cancellationToken = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromMilliseconds(_options.WriteTimeoutMs));

        var response = await _httpClient.DeleteAsync(
            $"api/rest/worklogs/{Uri.EscapeDataString(worklogId)}?api-version={ApiVersion}",
            cts.Token);

        await EnsureSuccessStatusCodeAsync(response, cts.Token);
    }

    // Time Report
    public async Task<JsonDocument> GetTimeReportAsync(
        string from,
        string to,
        string? userId = null,
        CancellationToken cancellationToken = default)
    {
        var query = $"api/rest/reports/time?api-version={ApiVersion}&from={Uri.EscapeDataString(from)}&to={Uri.EscapeDataString(to)}";
        if (!string.IsNullOrWhiteSpace(userId))
            query += $"&userId={Uri.EscapeDataString(userId)}";

        var response = await _httpClient.GetAsync(query, cancellationToken);
        await EnsureSuccessStatusCodeAsync(response, cancellationToken);

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonDocument.Parse(json);
    }

    private async Task EnsureSuccessStatusCodeAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
            return;

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        _logger?.LogError("7Pace API error: {StatusCode} {ReasonPhrase}. Body: {Body}", (int)response.StatusCode, response.ReasonPhrase, body);
        throw new HttpRequestException($"7Pace API error ({(int)response.StatusCode}): {response.ReasonPhrase} - {body}");
    }
}