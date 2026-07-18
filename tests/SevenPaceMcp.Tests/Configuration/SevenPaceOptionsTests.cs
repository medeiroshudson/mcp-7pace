using SevenPace.McpServer.Configuration;

namespace SevenPaceMcp.Tests.Configuration;

public class SevenPaceOptionsTests
{
    [Fact]
    public void IsLimitedMode_ReturnsTrue_WhenTokenIsNull()
    {
        // Arrange
        var options = new SevenPaceOptions { Organization = "org", Token = null };

        // Act & Assert
        Assert.True(options.IsLimitedMode);
    }

    [Fact]
    public void IsLimitedMode_ReturnsTrue_WhenTokenIsEmpty()
    {
        // Arrange
        var options = new SevenPaceOptions { Organization = "org", Token = string.Empty };

        // Act & Assert
        Assert.True(options.IsLimitedMode);
    }

    [Fact]
    public void IsLimitedMode_ReturnsTrue_WhenTokenIsWhitespace()
    {
        // Arrange
        var options = new SevenPaceOptions { Organization = "org", Token = "   " };

        // Act & Assert
        Assert.True(options.IsLimitedMode);
    }

    [Fact]
    public void IsLimitedMode_ReturnsTrue_WhenTokenIsPlaceholder()
    {
        // Arrange
        var options = new SevenPaceOptions
        {
            Organization = "org",
            Token = SevenPaceOptions.PlaceholderToken
        };

        // Act & Assert
        Assert.True(options.IsLimitedMode);
    }

    [Fact]
    public void IsLimitedMode_ReturnsFalse_WhenTokenIsValid()
    {
        // Arrange
        var options = new SevenPaceOptions { Organization = "org", Token = "valid-token-123" };

        // Act & Assert
        Assert.False(options.IsLimitedMode);
    }

    [Fact]
    public void ResolveBaseUrl_ReturnsDefault_WhenBaseUrlNotSet()
    {
        // Arrange
        var options = new SevenPaceOptions { Organization = "myorg", BaseUrl = null };

        // Act
        var url = options.ResolveBaseUrl();

        // Assert
        Assert.Equal("https://myorg.timehub.7pace.com", url);
    }

    [Fact]
    public void ResolveBaseUrl_ReturnsExplicit_WhenBaseUrlSet()
    {
        // Arrange
        var options = new SevenPaceOptions
        {
            Organization = "myorg",
            BaseUrl = "https://custom.example.com"
        };

        // Act
        var url = options.ResolveBaseUrl();

        // Assert
        Assert.Equal("https://custom.example.com", url);
    }

    [Fact]
    public void WriteTimeoutMs_DefaultsTo30000()
    {
        // Arrange & Act
        var options = new SevenPaceOptions { Organization = "org" };

        // Assert
        Assert.Equal(30000, options.WriteTimeoutMs);
    }

    [Fact]
    public void ResolveUtcOffset_ReturnsFallback_WhenTimeZoneIsNull()
    {
        // Arrange
        var options = new SevenPaceOptions { Organization = "org", TimeZone = null };

        // Act
        var offset = options.ResolveUtcOffset();

        // Assert
        Assert.Equal(TimeSpan.FromHours(-3), offset);
    }

    [Fact]
    public void ResolveUtcOffset_ReturnsFallback_WhenTimeZoneIsInvalid()
    {
        // Arrange
        var options = new SevenPaceOptions { Organization = "org", TimeZone = "Invalid/Zone" };

        // Act
        var offset = options.ResolveUtcOffset();

        // Assert
        Assert.Equal(TimeSpan.FromHours(-3), offset);
    }

    [Fact]
    public void ResolveUtcOffset_ReturnsCorrectOffset_WhenTimeZoneIsUtc()
    {
        // Arrange
        var options = new SevenPaceOptions { Organization = "org", TimeZone = "UTC" };

        // Act
        var offset = options.ResolveUtcOffset();

        // Assert
        Assert.Equal(TimeSpan.Zero, offset);
    }
}