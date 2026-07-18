using System.Text.Json.Serialization;

namespace SevenPace.McpServer.Dtos;

public sealed record WorklogItem
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("workItemId")]
    public int? WorkItemId { get; init; }

    [JsonPropertyName("timestamp")]
    public string? Timestamp { get; init; }

    [JsonPropertyName("length")]
    public int? Length { get; init; }

    [JsonPropertyName("comment")]
    public string? Comment { get; init; }

    [JsonPropertyName("activityTypeId")]
    public string? ActivityTypeId { get; init; }
}

public sealed record WorklogListResponse
{
    [JsonPropertyName("data")]
    public List<WorklogItem>? Data { get; init; }

    [JsonPropertyName("value")]
    public List<WorklogItem>? Value { get; init; }
}