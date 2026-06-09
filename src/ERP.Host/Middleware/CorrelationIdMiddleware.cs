using ERP.Shared.Application.Abstractions;
using Serilog.Context;

namespace ERP.Host.Middleware;

public sealed class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ICurrentTenant currentTenant, ICurrentUser currentUser)
    {
        var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault()
                         ?? Guid.NewGuid().ToString("N")[..16];

        context.Response.Headers["X-Correlation-ID"] = correlationId;
        context.TraceIdentifier = correlationId;

        var sw = System.Diagnostics.Stopwatch.StartNew();

        using (LogContext.PushProperty("CorrelationId", correlationId))
        using (LogContext.PushProperty("TenantId", currentTenant.TenantId?.ToString() ?? "none"))
        using (LogContext.PushProperty("UserId", currentUser?.UserId?.ToString() ?? "anonymous"))
        using (LogContext.PushProperty("RequestPath", context.Request.Path))
        using (LogContext.PushProperty("RequestMethod", context.Request.Method))
        {
            try
            {
                await _next(context);
            }
            finally
            {
                sw.Stop();
                // AKS liveness/readiness probes hit /health every few seconds — suppress to avoid log flood
                if (!context.Request.Path.StartsWithSegments("/health"))
                {
                    _logger.LogInformation(
                        "HTTP {Method} {Path} responded {StatusCode} in {DurationMs}ms | tenant={TenantId} user={UserId} correlation={CorrelationId}",
                        context.Request.Method,
                        context.Request.Path,
                        context.Response.StatusCode,
                        sw.ElapsedMilliseconds,
                        currentTenant.TenantId,
                        currentUser?.UserId,
                        correlationId);
                }
            }
        }
    }
}
