using System.Text.Json.Serialization;

namespace SevenPace.McpServer.Dtos;

public sealed record ReportResponse
{
    [JsonPropertyName("totalHours")]
    public double? TotalHours { get; init; }

    [JsonPropertyName("totalEntries")]
    public int? TotalEntries { get; init; }

    [JsonPropertyName("data")]
    public object? Data { get; init; }
}