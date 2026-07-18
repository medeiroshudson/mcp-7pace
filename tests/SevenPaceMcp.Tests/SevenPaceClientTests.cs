using System.Net;
using System.Text;
using System.Text.Json;
using SevenPace.McpServer;
using SevenPace.McpServer.Configuration;
using SevenPace.McpServer.Dtos;

namespace SevenPaceMcp.Tests;

public class SevenPaceClientTests
{
    private sealed class FakeHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _handler;
        public HttpRequestMessage? LastRequest { get; private set; }
        public string? LastRequestBody { get; private set; }

        public FakeHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            if (request.Content is not null)
                LastRequestBody = await request.Content.ReadAsStringAsync(cancellationToken);
            return await _handler(request);
        }
    }

    private static SevenPaceOptions ValidOptions() => new()
    {
        Organization = "testorg",
        Token = "valid-token"
    };

    private static HttpResponseMessage JsonResponse(string body, HttpStatusCode code = HttpStatusCode.OK) =>
        new(code) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    [Fact]
    public async Task GetActivityTypesAsync_SendsBearerAuthAndCorrectUrl()
    {
        var handler = new FakeHandler(_ => Task.FromResult(JsonResponse("""{"data":{"activityTypes":[{"id":"abc","name":"Dev"}]}}""")));
        var client = new SevenPaceClient(new HttpClient(handler), ValidOptions());

        var result = await client.GetActivityTypesAsync();

        Assert.Equal("Bearer valid-token", handler.LastRequest!.Headers.Authorization!.ToString());
        Assert.Contains("api/rest/activitytypes", handler.LastRequest!.RequestUri!.ToString());
        Assert.Contains("api-version=3.2", handler.LastRequest!.RequestUri!.ToString());
        Assert.Single(result);
        Assert.Equal("abc", result[0].Id);
        Assert.Equal("Dev", result[0].Name);
    }

    [Fact]
    public async Task CreateWorklogAsync_SendsPostWithCorrectPayload()
    {
        var handler = new FakeHandler(_ => Task.FromResult(JsonResponse("""{"id":"wl-123"}""")));
        var client = new SevenPaceClient(new HttpClient(handler), ValidOptions());

        var request = new CreateWorklogRequest
        {
            WorkItemId = 42,
            TimeStamp = "2026-07-17T00:00:00-03:00",
            Length = 3600,
            Comment = "Test work"
        };

        await client.CreateWorklogAsync(request);

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("api/rest/workLogs", handler.LastRequest!.RequestUri!.ToString());
        Assert.Contains("api-version=3.2", handler.LastRequest!.RequestUri!.ToString());
        Assert.Contains("workItemId", handler.LastRequestBody!);
        Assert.Contains("timeStamp", handler.LastRequestBody!);
        Assert.Contains("3600", handler.LastRequestBody!);
        // Verify billableLength is NOT in the payload
        Assert.DoesNotContain("billableLength", handler.LastRequestBody!);
    }

    [Fact]
    public async Task UpdateWorklogAsync_SendsPutNotPatch()
    {
        var handler = new FakeHandler(_ => Task.FromResult(JsonResponse("""{"id":"wl-123"}""")));
        var client = new SevenPaceClient(new HttpClient(handler), ValidOptions());

        var request = new UpdateWorklogRequest { Length = 7200 };

        await client.UpdateWorklogAsync("wl-123", request);

        Assert.Equal(HttpMethod.Put, handler.LastRequest!.Method);
        Assert.Contains("api/rest/worklogs/wl-123", handler.LastRequest!.RequestUri!.ToString());
        Assert.DoesNotContain("billableLength", handler.LastRequestBody!);
    }

    [Fact]
    public async Task DeleteWorklogAsync_SendsDeleteRequest()
    {
        var handler = new FakeHandler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
        var client = new SevenPaceClient(new HttpClient(handler), ValidOptions());

        await client.DeleteWorklogAsync("wl-456");

        Assert.Equal(HttpMethod.Delete, handler.LastRequest!.Method);
        Assert.Contains("api/rest/worklogs/wl-456", handler.LastRequest!.RequestUri!.ToString());
    }

    [Fact]
    public async Task GetWorklogsAsync_PassesCorrectUrlAndParams()
    {
        var handler = new FakeHandler(_ => Task.FromResult(JsonResponse("""{"data":[]}""")));
        var client = new SevenPaceClient(new HttpClient(handler), ValidOptions());

        await client.GetWorklogsAsync(
            workItemId: 123,
            fromTimestamp: "2026-01-01T00:00:00-03:00",
            toTimestamp: "2026-01-31T23:59:59-03:00");

        var uri = handler.LastRequest!.RequestUri!.ToString();
        // URL must use camelCase workLogs (not lowercase worklogs)
        Assert.Contains("api/rest/workLogs", uri);
        Assert.Contains("api-version=3.2", uri);
        // Parameters must use $ prefix and correct names
        Assert.Contains("$workItemIds=123", uri);
        Assert.Contains("$fromTimestamp=", uri);
        Assert.Contains("$toTimestamp=", uri);
        // Pagination params
        Assert.Contains("$count=500", uri);
        Assert.Contains("$skip=0", uri);
    }

    [Fact]
    public async Task GetWorklogsAsync_PassesCreatedTimestampParams()
    {
        var handler = new FakeHandler(_ => Task.FromResult(JsonResponse("""{"data":[]}""")));
        var client = new SevenPaceClient(new HttpClient(handler), ValidOptions());

        await client.GetWorklogsAsync(
            fromCreatedTimestamp: "2026-01-01T00:00:00-03:00",
            toCreatedTimestamp: "2026-01-31T23:59:59-03:00");

        var uri = handler.LastRequest!.RequestUri!.ToString();
        Assert.Contains("$fromCreatedTimestamp=", uri);
        Assert.Contains("$toCreatedTimestamp=", uri);
    }

    [Fact]
    public async Task GetWorklogsAsync_PaginatesUntilLastPage()
    {
        // First page returns 500 items (full page), second returns 0 (last page).
        var callCount = 0;
        var handler = new FakeHandler(_ =>
        {
            callCount++;
            var body = callCount == 1
                ? """{"data":[""" + string.Join(",", Enumerable.Repeat("""{"id":"wl"}""", 500)) + """]}"""
                : """{"data":[]}""";
            return Task.FromResult(JsonResponse(body));
        });
        var client = new SevenPaceClient(new HttpClient(handler), ValidOptions());

        var result = await client.GetWorklogsAsync();

        Assert.Equal(2, callCount);
        Assert.Equal(500, result.Count);
    }
}