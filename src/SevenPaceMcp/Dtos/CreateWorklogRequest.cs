using System.Text.Json.Serialization;

namespace SevenPace.McpServer.Dtos;

/// <summary>
/// Payload for POST /api/rest/workLogs?api-version=3.2
/// Uses the official "timeStamp" field (ISO 8601 with offset) instead of
/// the non-standard "date" field, ensuring the 7Pace API records the
/// correct date regardless of server time or timezone.
/// </summary>
public sealed record CreateWorklogRequest
{
    [JsonPropertyName("workItemId")]
    public int WorkItemId { get; init; }

    [JsonPropertyName("timeStamp")]
    public string TimeStamp { get; init; } = string.Empty;

    [JsonPropertyName("length")]
    public int Length { get; init; }

    [JsonPropertyName("comment")]
    public string Comment { get; init; } = string.Empty;

    [JsonPropertyName("activityTypeId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ActivityTypeId { get; init; }
}