using ERP.Shared.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace ERP.Host.Middleware;

public sealed class NullWhatsAppService : IWhatsAppService
{
    private readonly ILogger<NullWhatsAppService> _logger;
    public NullWhatsAppService(ILogger<NullWhatsAppService> logger) => _logger = logger;

    public Task SendOtpAsync(string toMobileNumber, string otp, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("[WHATSAPP-NULL] OTP {Otp} → {Mobile}", otp, toMobileNumber);
        return Task.CompletedTask;
    }
}
