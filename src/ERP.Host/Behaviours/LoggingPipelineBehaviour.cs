using MediatR;
using Serilog.Context;

namespace ERP.Host.Behaviours;

public sealed class LoggingPipelineBehaviour<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingPipelineBehaviour<TRequest, TResponse>> _logger;

    public LoggingPipelineBehaviour(ILogger<LoggingPipelineBehaviour<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        // Derive module from namespace: ERP.<Module>.Application → <Module>
        var moduleName  = typeof(TRequest).Namespace?.Split('.').Skip(1).FirstOrDefault() ?? "Unknown";

        var sw = System.Diagnostics.Stopwatch.StartNew();

        using (LogContext.PushProperty("Module", moduleName))
        using (LogContext.PushProperty("Action", requestName))
        {
            try
            {
                _logger.LogDebug("Handling {Action} in {Module}", requestName, moduleName);
                var response = await next();
                sw.Stop();
                _logger.LogDebug("Handled {Action} in {DurationMs}ms", requestName, sw.ElapsedMilliseconds);
                return response;
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(ex, "Failed {Action} in {Module} after {DurationMs}ms", requestName, moduleName, sw.ElapsedMilliseconds);
                throw;
            }
        }
    }
}
