using System.ComponentModel.DataAnnotations;

namespace SevenPace.McpServer.Configuration;

/// <summary>
/// Configuration for 7Pace Timetracker API connection.
/// Bound from environment variables: SEVENPACE_ORGANIZATION, SEVENPACE_TOKEN,
/// SEVENPACE_BASE_URL, SEVENPACE_WRITE_TIMEOUT_MS.
/// </summary>
public sealed class SevenPaceOptions
{
    [Required(ErrorMessage = "SEVENPACE_ORGANIZATION is required. Set it as an environment variable.")]
    public string Organization { get; init; } = string.Empty;

    /// <summary>
    /// 7Pace API token. When null, empty, or the placeholder value,
    /// the server runs in limited mode (no API calls).
    /// </summary>
    public string? Token { get; init; }

    /// <summary>
    /// Base URL for the 7Pace API. Defaults to https://{organization}.timehub.7pace.com
    /// </summary>
    public string? BaseUrl { get; init; }

    /// <summary>
    /// Timeout in milliseconds for write operations (create/update/delete).
    /// Default: 30000 (30 seconds).
    /// </summary>
    [Range(1000, 300000)]
    public int WriteTimeoutMs { get; init; } = 30000;

    /// <summary>
    /// IANA timezone ID (e.g., "America/Sao_Paulo") used when converting
    /// date-only inputs to ISO 8601 timestamps for the 7Pace API.
    /// Falls back to UTC-3 when not set or invalid.
    /// </summary>
    public string? TimeZone { get; init; }

    /// <summary>
    /// Fallback offset when TimeZone is not set or cannot be resolved.
    /// </summary>
    private static readonly TimeSpan FallbackOffset = TimeSpan.FromHours(-3);

    /// <summary>
    /// Resolves the UTC offset for the configured timezone at the current moment.
    /// Falls back to UTC-3 when TimeZone is null, empty, or not a valid IANA ID.
    /// </summary>
    public TimeSpan ResolveUtcOffset()
    {
        if (string.IsNullOrWhiteSpace(TimeZone))
            return FallbackOffset;

        try
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById(TimeZone);
            return tz.GetUtcOffset(DateTimeOffset.UtcNow);
        }
        catch
        {
            return FallbackOffset;
        }
    }

    /// <summary>
    /// The placeholder token used by deployment scanners. When Token equals this,
    /// the server is in limited mode.
    /// </summary>
    public const string PlaceholderToken = "test-token-replace-with-real-token";

    /// <summary>
    /// Returns true when the server should operate in limited mode:
    /// token is null, empty, whitespace, or the placeholder value.
    /// </summary>
    public bool IsLimitedMode =>
        string.IsNullOrWhiteSpace(Token) || Token == PlaceholderToken;

    /// <summary>
    /// Resolves the effective base URL: explicit override or default from organization.
    /// </summary>
    public string ResolveBaseUrl() =>
        !string.IsNullOrWhiteSpace(BaseUrl)
            ? BaseUrl!
            : $"https://{Organization}.timehub.7pace.com";
}