using System.Text.Json.Serialization;

namespace SevenPace.McpServer.Dtos;

/// <summary>
/// Payload for PUT /api/rest/worklogs/{id}?api-version=3.2
/// </summary>
public sealed record UpdateWorklogRequest
{
    [JsonPropertyName("length")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Length { get; init; }

    [JsonPropertyName("comment")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Comment { get; init; }

    [JsonPropertyName("workItemId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? WorkItemId { get; init; }
}