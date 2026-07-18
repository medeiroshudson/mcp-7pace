using System.Net;
using System.Text;

namespace SevenPaceMcp.Tests.Helpers;

/// <summary>
/// Fake HttpMessageHandler that returns a configured response for every request,
/// allowing services to be tested through the real SevenPaceClient without network access.
/// </summary>
internal sealed class FakeHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _responder;

    public FakeHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> responder)
    {
        _responder = responder;
    }

    public FakeHandler(HttpResponseMessage response)
        : this(_ => Task.FromResult(response))
    {
    }

    /// <summary>
    /// Creates a handler that returns 200 OK with the given JSON body.
    /// </summary>
    public static FakeHandler FromJson(string json, HttpStatusCode status = HttpStatusCode.OK)
    {
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return new FakeHandler(_ => Task.FromResult(new HttpResponseMessage(status) { Content = content }));
    }

    /// <summary>
    /// Creates a handler that returns an empty 200 OK response.
    /// </summary>
    public static FakeHandler Empty(HttpStatusCode status = HttpStatusCode.OK)
        => new(new HttpResponseMessage(status));

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
        => _responder(request);
}

/// <summary>
/// Reflection helpers for asserting against anonymous-typed OperationResult.Data.
/// </summary>
internal static class ResultDataExtensions
{
    public static object? GetProperty(this object? source, string name)
    {
        if (source is null)
            return null;
        var prop = source.GetType().GetProperty(name);
        return prop?.GetValue(source);
    }

    public static T? GetProperty<T>(this object? source, string name)
    {
        var value = GetProperty(source, name);
        return value is null ? default : (T)value;
    }
}