namespace SevenPace.McpServer;

public sealed class OperationResult(bool success, string? error = null, object? data = null)
{
    public bool Success { get; } = success;
    public string? Error { get; } = error;
    public object? Data { get; } = data;

    public static OperationResult Ok(object? data = null) => new(true, data: data);
    public static OperationResult Fail(string error) => new(false, error);
}