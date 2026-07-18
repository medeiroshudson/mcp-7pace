using SevenPace.McpServer;

namespace SevenPaceMcp.Tests;

public class OperationResultTests
{
    [Fact]
    public void Ok_WithNoData_ReturnsSuccessWithNullData()
    {
        // Arrange & Act
        var result = OperationResult.Ok();

        // Assert
        Assert.True(result.Success);
        Assert.Null(result.Error);
        Assert.Null(result.Data);
    }

    [Fact]
    public void Ok_WithData_ReturnsSuccessWithData()
    {
        // Arrange
        var data = new { value = 42 };

        // Act
        var result = OperationResult.Ok(data);

        // Assert
        Assert.True(result.Success);
        Assert.Null(result.Error);
        Assert.Same(data, result.Data);
    }

    [Fact]
    public void Fail_ReturnsFailureWithError()
    {
        // Arrange
        const string error = "Something went wrong";

        // Act
        var result = OperationResult.Fail(error);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(error, result.Error);
        Assert.Null(result.Data);
    }
}