using System.Text.Json.Serialization;

namespace SevenPace.McpServer.Dtos;

public sealed record ActivityType
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;
}

public sealed record ActivityTypeResponse
{
    [JsonPropertyName("data")]
    public ActivityTypeResponseData? Data { get; init; }
}

public sealed record ActivityTypeResponseData
{
    [JsonPropertyName("enabled")]
    public bool? Enabled { get; init; }

    [JsonPropertyName("activityTypes")]
    public List<ActivityType>? ActivityTypes { get; init; }
}