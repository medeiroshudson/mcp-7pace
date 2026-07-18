using System.Net;
using SevenPace.McpServer;
using SevenPace.McpServer.Configuration;
using SevenPace.McpServer.Services;
using SevenPaceMcp.Tests.Helpers;

namespace SevenPaceMcp.Tests.Services;

public class ReportServiceTests
{
    private static SevenPaceOptions ValidOptions() =>
        new() { Organization = "test", Token = "valid-token" };

    private static SevenPaceOptions LimitedOptions() =>
        new() { Organization = "test", Token = SevenPaceOptions.PlaceholderToken };

    private static ReportService CreateService(SevenPaceOptions options, FakeHandler? handler = null)
    {
        handler ??= FakeHandler.Empty();
        var client = new SevenPaceClient(new HttpClient(handler), options);
        return new ReportService(client, options);
    }

    [Fact]
    public async Task GenerateTimeReportAsync_LimitedMode_ReturnsInformationalResult()
    {
        // Arrange
        var service = CreateService(LimitedOptions());

        // Act
        var result = await service.GenerateTimeReportAsync(
            new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2025, 1, 31, 0, 0, 0, TimeSpan.Zero));

        // Assert
        Assert.True(result.Success);
        Assert.Null(result.Error);
        Assert.True(result.Data.GetProperty<bool>("limitedMode"));
    }

    [Fact]
    public async Task GenerateTimeReportAsync_StartDateAfterEndDate_ReturnsFail()
    {
        // Arrange
        var service = CreateService(ValidOptions());

        // Act
        var result = await service.GenerateTimeReportAsync(
            new DateTimeOffset(2025, 2, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero));

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.Error);
    }
}