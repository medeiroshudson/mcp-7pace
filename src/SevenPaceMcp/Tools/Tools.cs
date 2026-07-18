using Microsoft.Extensions.Logging;
using SevenPace.McpServer.Configuration;
using SevenPace.McpServer.Services;
using ModelContextProtocol.Server;

namespace SevenPace.McpServer;

/// <summary>
/// MCP tool surface for 7pace Timetracker. This is the partial class base that
/// receives all services via DI; individual tool methods live in sibling partials
/// (e.g. <see cref="WorklogTools"/>, ReportTools). The MCP SDK discovers every
/// method annotated with <c>[McpServerTool]</c> across all partials.
/// </summary>
[McpServerToolType]
public sealed partial class Tools
{
    private readonly WorklogService _worklogService;
    private readonly ActivityTypeService _activityTypeService;
    private readonly ReportService _reportService;
    private readonly SevenPaceOptions _options;
    private readonly ILogger<Tools> _logger;

    public Tools(
        WorklogService worklogService,
        ActivityTypeService activityTypeService,
        ReportService reportService,
        SevenPaceOptions options,
        ILogger<Tools> logger)
    {
        _worklogService = worklogService;
        _activityTypeService = activityTypeService;
        _reportService = reportService;
        _options = options;
        _logger = logger;
    }
}