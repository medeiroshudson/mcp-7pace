using System.Net;
using SevenPace.McpServer;
using SevenPace.McpServer.Configuration;
using SevenPace.McpServer.Services;
using SevenPaceMcp.Tests.Helpers;

namespace SevenPaceMcp.Tests.Services;

public class WorklogServiceTests
{
    private static SevenPaceOptions ValidOptions() =>
        new() { Organization = "test", Token = "valid-token" };

    private static SevenPaceOptions LimitedOptions() =>
        new() { Organization = "test", Token = SevenPaceOptions.PlaceholderToken };

    private static WorklogService CreateService(SevenPaceOptions options, FakeHandler? handler = null)
    {
        handler ??= FakeHandler.Empty();
        var client = new SevenPaceClient(new HttpClient(handler), options);
        return new WorklogService(client, options);
    }

    private static readonly DateTimeOffset TestDate = new(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);

    // --- ComputeHoursFromApiLength ---

    [Fact]
    public void ComputeHoursFromApiLength_ReturnsSeconds_WhenLengthIsLarge()
    {
        // Arrange & Act
        var hours = WorklogService.ComputeHoursFromApiLength(3600);

        // Assert
        Assert.Equal(1.0, hours);
    }

    [Fact]
    public void ComputeHoursFromApiLength_ReturnsMinutes_WhenLengthIsSmall()
    {
        // Arrange & Act
        var hours = WorklogService.ComputeHoursFromApiLength(30);

        // Assert
        Assert.Equal(0.5, hours);
    }

    // --- DateTimeOffset formatting ---

    [Fact]
    public void DateTimeOffset_FormatsIso8601WithOffset()
    {
        // Arrange
        var date = new DateTimeOffset(2026, 7, 16, 0, 0, 0, TimeSpan.FromHours(-3));

        // Act
        var result = date.ToString("yyyy-MM-ddTHH:mm:sszzz");

        // Assert
        Assert.Equal("2026-07-16T00:00:00-03:00", result);
    }

    [Fact]
    public void DateTimeOffset_FormatsWithUtcZeroOffset()
    {
        // Arrange
        var date = new DateTimeOffset(2026, 7, 16, 0, 0, 0, TimeSpan.Zero);

        // Act
        var result = date.ToString("yyyy-MM-ddTHH:mm:sszzz");

        // Assert
        Assert.Equal("2026-07-16T00:00:00+00:00", result);
    }

    [Fact]
    public void DateTimeOffset_AddSecondsMinusOne_ProducesPreviousSecond()
    {
        // Arrange — -1 second makes the range inclusive for exclusive API filters
        var date = new DateTimeOffset(2026, 7, 16, 0, 0, 0, TimeSpan.FromHours(-3));

        // Act
        var result = date.AddSeconds(-1).ToString("yyyy-MM-ddTHH:mm:sszzz");

        // Assert
        Assert.Equal("2026-07-15T23:59:59-03:00", result);
    }

    [Fact]
    public void DateTimeOffset_EndOfDay_ProducesEndOfDay()
    {
        // Arrange — end of day for inclusive upper bound
        var date = new DateTimeOffset(2026, 7, 16, 0, 0, 0, TimeSpan.FromHours(-3));

        // Act — construct end-of-day preserving the original offset
        var endOfDay = new DateTimeOffset(date.Year, date.Month, date.Day, 23, 59, 59, date.Offset);
        var result = endOfDay.ToString("yyyy-MM-ddTHH:mm:sszzz");

        // Assert
        Assert.Equal("2026-07-16T23:59:59-03:00", result);
    }

    // --- LogTimeAsync ---

    [Fact]
    public async Task LogTimeAsync_LimitedMode_ReturnsInformationalResult()
    {
        // Arrange
        var service = CreateService(LimitedOptions());

        // Act
        var result = await service.LogTimeAsync(1, TestDate, 1.0, "test");

        // Assert
        Assert.True(result.Success);
        Assert.Null(result.Error);
        Assert.True(result.Data.GetProperty<bool>("limitedMode"));
    }

    [Fact]
    public async Task LogTimeAsync_InvalidWorkItemId_ReturnsFail()
    {
        // Arrange
        var service = CreateService(ValidOptions());

        // Act
        var result = await service.LogTimeAsync(0, TestDate, 1.0, "test");

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task LogTimeAsync_NegativeHours_ReturnsFail()
    {
        // Arrange
        var service = CreateService(ValidOptions());

        // Act
        var result = await service.LogTimeAsync(1, TestDate, -1, "test");

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task LogTimeAsync_EmptyDescription_ReturnsFail()
    {
        // Arrange
        var service = CreateService(ValidOptions());

        // Act
        var result = await service.LogTimeAsync(1, TestDate, 1.0, "");

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.Error);
    }

    // --- GetWorklogsAsync ---

    [Fact]
    public async Task GetWorklogsAsync_LimitedMode_ReturnsInformationalResult()
    {
        // Arrange
        var service = CreateService(LimitedOptions());

        // Act
        var result = await service.GetWorklogsAsync();

        // Assert
        Assert.True(result.Success);
        Assert.Null(result.Error);
        Assert.True(result.Data.GetProperty<bool>("limitedMode"));
    }

    [Fact]
    public async Task GetWorklogsAsync_StartDateAfterEndDate_ReturnsFail()
    {
        // Arrange
        var service = CreateService(ValidOptions());

        // Act
        var result = await service.GetWorklogsAsync(
            startDate: new DateTimeOffset(2025, 2, 1, 0, 0, 0, TimeSpan.Zero),
            endDate: new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero));

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task GetWorklogsAsync_CreatedStartDateAfterCreatedEndDate_ReturnsFail()
    {
        // Arrange
        var service = CreateService(ValidOptions());

        // Act
        var result = await service.GetWorklogsAsync(
            createdStartDate: new DateTimeOffset(2025, 2, 1, 0, 0, 0, TimeSpan.Zero),
            createdEndDate: new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero));

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.Error);
    }

    // --- UpdateWorklogAsync ---

    [Fact]
    public async Task UpdateWorklogAsync_LimitedMode_ReturnsInformationalResult()
    {
        // Arrange
        var service = CreateService(LimitedOptions());

        // Act
        var result = await service.UpdateWorklogAsync("wl-1", workItemId: 1, hours: 1.0);

        // Assert
        Assert.True(result.Success);
        Assert.Null(result.Error);
        Assert.True(result.Data.GetProperty<bool>("limitedMode"));
    }

    [Fact]
    public async Task UpdateWorklogAsync_EmptyWorklogId_ReturnsFail()
    {
        // Arrange
        var service = CreateService(ValidOptions());

        // Act
        var result = await service.UpdateWorklogAsync("", workItemId: 1, hours: 1.0);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task UpdateWorklogAsync_NoFieldsToUpdate_ReturnsFail()
    {
        // Arrange
        var service = CreateService(ValidOptions());

        // Act
        var result = await service.UpdateWorklogAsync("wl-1");

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.Error);
    }

    // --- DeleteWorklogAsync ---

    [Fact]
    public async Task DeleteWorklogAsync_LimitedMode_ReturnsInformationalResult()
    {
        // Arrange
        var service = CreateService(LimitedOptions());

        // Act
        var result = await service.DeleteWorklogAsync("wl-1");

        // Assert
        Assert.True(result.Success);
        Assert.Null(result.Error);
        Assert.True(result.Data.GetProperty<bool>("limitedMode"));
    }

    [Fact]
    public async Task DeleteWorklogAsync_EmptyWorklogId_ReturnsFail()
    {
        // Arrange
        var service = CreateService(ValidOptions());

        // Act
        var result = await service.DeleteWorklogAsync("");

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.Error);
    }
}