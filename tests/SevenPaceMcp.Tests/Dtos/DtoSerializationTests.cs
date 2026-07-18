using System.Text.Json;
using SevenPace.McpServer.Dtos;

namespace SevenPaceMcp.Tests.Dtos;

public class DtoSerializationTests
{
    private static string Serialize<T>(T value) where T : class
    {
        var typeInfo = SevenPaceJsonContext.Default.GetTypeInfo(typeof(T));
        return JsonSerializer.Serialize(value, typeInfo!);
    }

    [Fact]
    public void CreateWorklogRequest_SerializesWithTimeStampField()
    {
        // Arrange
        var request = new CreateWorklogRequest
        {
            WorkItemId = 42,
            TimeStamp = "2026-07-17T00:00:00-03:00",
            Length = 3600,
            Comment = "Test work"
        };

        // Act
        var json = Serialize(request);

        // Assert
        Assert.Contains("\"timeStamp\":\"2026-07-17T00:00:00-03:00\"", json);
        Assert.DoesNotContain("\"date\"", json);
    }

    [Fact]
    public void CreateWorklogRequest_DoesNotContainBillableLength()
    {
        // Arrange
        var request = new CreateWorklogRequest
        {
            WorkItemId = 42,
            TimeStamp = "2026-07-17T00:00:00-03:00",
            Length = 3600,
            Comment = "Test work"
        };

        // Act
        var json = Serialize(request);

        // Assert
        Assert.DoesNotContain("billableLength", json);
    }

    [Fact]
    public void CreateWorklogRequest_OmitsActivityTypeId_WhenNull()
    {
        // Arrange
        var request = new CreateWorklogRequest
        {
            WorkItemId = 42,
            TimeStamp = "2026-07-17T00:00:00-03:00",
            Length = 3600,
            Comment = "Test work",
            ActivityTypeId = null
        };

        // Act
        var json = Serialize(request);

        // Assert
        Assert.DoesNotContain("activityTypeId", json);
    }

    [Fact]
    public void CreateWorklogRequest_IncludesActivityTypeId_WhenSet()
    {
        // Arrange
        var request = new CreateWorklogRequest
        {
            WorkItemId = 42,
            TimeStamp = "2026-07-17T00:00:00-03:00",
            Length = 3600,
            Comment = "Test work",
            ActivityTypeId = "dev-activity"
        };

        // Act
        var json = Serialize(request);

        // Assert
        Assert.Contains("\"activityTypeId\":\"dev-activity\"", json);
    }

    [Fact]
    public void UpdateWorklogRequest_DoesNotContainBillableLength()
    {
        // Arrange
        var request = new UpdateWorklogRequest
        {
            Length = 7200,
            Comment = "Updated",
            WorkItemId = 42
        };

        // Act
        var json = Serialize(request);

        // Assert
        Assert.DoesNotContain("billableLength", json);
    }

    [Fact]
    public void UpdateWorklogRequest_OmitsNullFields()
    {
        // Arrange
        var request = new UpdateWorklogRequest
        {
            Length = 7200,
            Comment = null,
            WorkItemId = null
        };

        // Act
        var json = Serialize(request);

        // Assert
        Assert.Contains("\"length\":7200", json);
        Assert.DoesNotContain("comment", json);
        Assert.DoesNotContain("workItemId", json);
    }
}