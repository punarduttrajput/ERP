namespace ERP.Tenants.API;

public static class ApiResponse
{
    public static object Ok<T>(T data, string traceId) => new
    {
        success = true,
        data,
        traceId
    };

    public static object Fail(string message, string traceId, IEnumerable<string>? errors = null) => new
    {
        success = false,
        message,
        errors = errors ?? Array.Empty<string>(),
        traceId
    };
}
