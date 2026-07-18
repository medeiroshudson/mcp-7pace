using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using SevenPace.McpServer;
using SevenPace.McpServer.Configuration;
using SevenPace.McpServer.Services;
using SevenPaceMcp.Tests.Helpers;

namespace SevenPaceMcp.Tests;

public class ToolsTests
{
    private static SevenPace.McpServer.Tools CreateTools(SevenPaceOptions options, FakeHandler? handler = null)
    {
        handler ??= FakeHandler.Empty();
        var client = new SevenPaceClient(new HttpClient(handler), options);
        var worklogService = new WorklogService(client, options);
        var activityTypeService = new ActivityTypeService(client, options);
        var reportService = new ReportService(client, options);
        var logger = NullLogger<SevenPace.McpServer.Tools>.Instance;
        return new SevenPace.McpServer.Tools(worklogService, activityTypeService, reportService, options, logger);
    }

    // --- Health ---

    [Fact]
    public void Health_ReturnsHealthyStatus()
    {
        // Arrange
        var options = new SevenPaceOptions { Organization = "test", Token = "valid-token" };
        var tools = CreateTools(options);

        // Act
        var result = tools.Health();

        // Assert
        using var doc = JsonDocument.Parse(result);
        Assert.Equal("healthy", doc.RootElement.GetProperty("status").GetString());
        Assert.False(doc.RootElement.GetProperty("limitedMode").GetBoolean());
    }

    [Fact]
    public void Health_ReturnsLimitedMode_WhenTokenIsPlaceholder()
    {
        // Arrange
        var options = new SevenPaceOptions { Organization = "test", Token = SevenPaceOptions.PlaceholderToken };
        var tools = CreateTools(options);

        // Act
        var result = tools.Health();

        // Assert
        using var doc = JsonDocument.Parse(result);
        Assert.Equal("healthy", doc.RootElement.GetProperty("status").GetString());
        Assert.True(doc.RootElement.GetProperty("limitedMode").GetBoolean());
    }

    // --- ConfigureSevenPace ---

    [Fact]
    public void ConfigureSevenPace_ReturnsError_WhenOrganizationIsEmpty()
    {
        // Arrange
        var options = new SevenPaceOptions { Organization = "test", Token = "valid-token" };
        var tools = CreateTools(options);

        // Act
        var result = tools.ConfigureSevenPace("", "token");

        // Assert
        using var doc = JsonDocument.Parse(result);
        Assert.False(doc.RootElement.GetProperty("success").GetBoolean());
        Assert.NotNull(doc.RootElement.GetProperty("error").GetString());
    }

    [Fact]
    public void ConfigureSevenPace_ReturnsError_WhenTokenIsEmpty()
    {
        // Arrange
        var options = new SevenPaceOptions { Organization = "test", Token = "valid-token" };
        var tools = CreateTools(options);

        // Act
        var result = tools.ConfigureSevenPace("org", "");

        // Assert
        using var doc = JsonDocument.Parse(result);
        Assert.False(doc.RootElement.GetProperty("success").GetBoolean());
        Assert.NotNull(doc.RootElement.GetProperty("error").GetString());
    }

    [Fact]
    public void ConfigureSevenPace_ReturnsSuccess_WhenValidInputs()
    {
        // Arrange
        var options = new SevenPaceOptions { Organization = "test", Token = "valid-token" };
        var tools = CreateTools(options);

        // Act
        var result = tools.ConfigureSevenPace("labournet", "secret-token");

        // Assert
        using var doc = JsonDocument.Parse(result);
        Assert.True(doc.RootElement.GetProperty("success").GetBoolean());
        Assert.NotNull(doc.RootElement.GetProperty("message").GetString());
        Assert.Equal(
            "https://labournet.timehub.7pace.com",
            doc.RootElement.GetProperty("baseUrl").GetString());
    }
}