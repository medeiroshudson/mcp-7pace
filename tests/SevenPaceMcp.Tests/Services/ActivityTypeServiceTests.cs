using System.Net;
using SevenPace.McpServer;
using SevenPace.McpServer.Configuration;
using SevenPace.McpServer.Services;
using SevenPaceMcp.Tests.Helpers;

namespace SevenPaceMcp.Tests.Services;

public class ActivityTypeServiceTests
{
    private static SevenPaceOptions ValidOptions() =>
        new() { Organization = "test", Token = "valid-token" };

    private static SevenPaceOptions LimitedOptions() =>
        new() { Organization = "test", Token = SevenPaceOptions.PlaceholderToken };

    private static ActivityTypeService CreateService(SevenPaceOptions options, FakeHandler? handler = null)
    {
        handler ??= FakeHandler.Empty();
        var client = new SevenPaceClient(new HttpClient(handler), options);
        return new ActivityTypeService(client, options);
    }

    [Fact]
    public async Task GetActivityTypesAsync_LimitedMode_ReturnsInformationalResult()
    {
        // Arrange
        var service = CreateService(LimitedOptions());

        // Act
        var result = await service.GetActivityTypesAsync();

        // Assert
        Assert.True(result.Success);
        Assert.Null(result.Error);
        Assert.True(result.Data.GetProperty<bool>("limitedMode"));
    }

    [Fact]
    public async Task ResolveActivityTypeIdAsync_ReturnsNull_WhenInputIsEmpty()
    {
        // Arrange
        var service = CreateService(ValidOptions());

        // Act
        var result = await service.ResolveActivityTypeIdAsync("");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ResolveActivityTypeIdAsync_ReturnsGuid_WhenInputIsGuid()
    {
        // Arrange
        var service = CreateService(ValidOptions());
        const string guid = "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee";

        // Act
        var result = await service.ResolveActivityTypeIdAsync(guid);

        // Assert
        Assert.Equal(guid, result);
    }

    [Fact]
    public async Task ResolveActivityTypeIdAsync_ReturnsNull_WhenInputIsUnknownName()
    {
        // Arrange — empty activity type list means no name match
        var handler = FakeHandler.FromJson("""{"data":{"enabled":true,"activityTypes":[]}}""");
        var service = CreateService(ValidOptions(), handler);

        // Act
        var result = await service.ResolveActivityTypeIdAsync("Unknown Activity");

        // Assert
        Assert.Null(result);
    }
}