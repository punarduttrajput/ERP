using ERP.Shared.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace ERP.Host.Middleware;

// Logs SMS instead of sending — replace with a real provider (Twilio, AWS SNS, etc.) for production.
public sealed class NullSmsService : ISmsService
{
    private readonly ILogger<NullSmsService> _logger;

    public NullSmsService(ILogger<NullSmsService> logger) => _logger = logger;

    public Task SendAsync(string toMobileNumber, string message, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("[SMS-NULL] To: {Mobile} | Message: {Message}", toMobileNumber, message);
        return Task.CompletedTask;
    }
}
