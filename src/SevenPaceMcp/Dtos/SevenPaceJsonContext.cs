using System.Text.Json.Serialization;

namespace SevenPace.McpServer.Dtos;

[JsonSerializable(typeof(CreateWorklogRequest))]
[JsonSerializable(typeof(UpdateWorklogRequest))]
[JsonSerializable(typeof(WorklogItem))]
[JsonSerializable(typeof(List<WorklogItem>))]
[JsonSerializable(typeof(WorklogListResponse))]
[JsonSerializable(typeof(ActivityType))]
[JsonSerializable(typeof(ActivityTypeResponse))]
[JsonSerializable(typeof(ActivityTypeResponseData))]
[JsonSerializable(typeof(ReportResponse))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = false)]
internal sealed partial class SevenPaceJsonContext : JsonSerializerContext;