using Microsoft.Extensions.Logging;
using SevenPace.McpServer.Configuration;
using SevenPace.McpServer.Dtos;

namespace SevenPace.McpServer.Services;

/// <summary>
/// Service for 7Pace activity types with in-memory caching.
/// Caches activity types for 5 minutes to avoid repeated API calls.
/// </summary>
public sealed class ActivityTypeService
{
    private readonly SevenPaceClient _client;
    private readonly SevenPaceOptions _options;
    private readonly ILogger<ActivityTypeService>? _logger;

    private ActivityType[]? _cachedItems;
    private DateTimeOffset _cacheTimestamp;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    public ActivityTypeService(SevenPaceClient client, SevenPaceOptions options, ILogger<ActivityTypeService>? logger = null)
    {
        _client = client;
        _options = options;
        _logger = logger;
    }

    /// <summary>
    /// Lists all available activity types (name and id).
    /// Uses in-memory cache with 5-minute TTL.
    /// </summary>
    public async Task<OperationResult> GetActivityTypesAsync(CancellationToken cancellationToken = default)
    {
        if (_options.IsLimitedMode)
        {
            return OperationResult.Ok(new
            {
                limitedMode = true,
                message = "No activity types available in limited mode. Set SEVENPACE_ORGANIZATION and SEVENPACE_TOKEN to enable."
            });
        }

        try
        {
            var items = await GetOrFetchActivityTypesAsync(cancellationToken);

            return OperationResult.Ok(new
            {
                count = items.Length,
                activityTypes = items.Select(a => new { a.Id, a.Name }).ToArray()
            });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "GetActivityTypes failed");
            return OperationResult.Fail($"Failed to list activity types: {ex.Message}");
        }
    }

    /// <summary>
    /// Resolves an activity type input (name or GUID) to its ID.
    /// Returns null if the input is empty or cannot be resolved.
    /// Invalid names are silently omitted (matching Node behavior).
    /// </summary>
    internal async Task<string?> ResolveActivityTypeIdAsync(string? input, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        var trimmed = input.Trim();

        // If it's already a GUID, return as-is
        if (IsGuidLike(trimmed))
            return trimmed;

        // Try to resolve by name (case-insensitive)
        try
        {
            var items = await GetOrFetchActivityTypesAsync(cancellationToken);
            var match = items.FirstOrDefault(a =>
                string.Equals(a.Name, trimmed, StringComparison.OrdinalIgnoreCase));
            return match?.Id;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to resolve activity type '{Input}'", input);
            return null;
        }
    }

    /// <summary>
    /// Returns cached activity types if fresh, otherwise fetches from API.
    /// </summary>
    private async Task<ActivityType[]> GetOrFetchActivityTypesAsync(CancellationToken cancellationToken)
    {
        if (_cachedItems is not null && DateTimeOffset.UtcNow - _cacheTimestamp < CacheTtl)
            return _cachedItems;

        var items = await _client.GetActivityTypesAsync(cancellationToken);
        _cachedItems = items;
        _cacheTimestamp = DateTimeOffset.UtcNow;
        return items;
    }

    /// <summary>
    /// Strict GUID format: xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx
    /// </summary>
    private static bool IsGuidLike(string value) =>
        System.Text.RegularExpressions.Regex.IsMatch(
            value,
            @"^[0-9a-fA-F]{8}-([0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12}$");
}