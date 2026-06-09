using System.Security.Cryptography;
using System.Text;
using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERP.Auth.Application.Commands;

public sealed class SendOtpHandler : IRequestHandler<SendOtpCommand, Result>
{
    private static readonly TimeSpan OtpTtl = TimeSpan.FromMinutes(5);

    private readonly IAuthDbContext _db;
    private readonly ICacheService _cache;
    private readonly IWhatsAppService _whatsApp;
    private readonly ICurrentTenant _currentTenant;
    private readonly ILogger<SendOtpHandler> _logger;

    public SendOtpHandler(
        IAuthDbContext db, ICacheService cache, IWhatsAppService whatsApp,
        ICurrentTenant currentTenant, ILogger<SendOtpHandler> logger)
    {
        _db = db;
        _cache = cache;
        _whatsApp = whatsApp;
        _currentTenant = currentTenant;
        _logger = logger;
    }

    public async Task<Result> Handle(SendOtpCommand request, CancellationToken cancellationToken)
    {
        if (!_currentTenant.TenantId.HasValue)
            return Result.Failure("Tenant context is required.");

        var mobile = request.MobileNumber.Trim();
        var userExists = await _db.Users
            .AnyAsync(u => u.MobileNumber == mobile && !u.IsDeleted && u.IsActive, cancellationToken);

        var otp = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(otp)));

        var cacheKey = $"otp:{_currentTenant.TenantId}:{mobile}";
        await _cache.SetAsync(cacheKey, hash, OtpTtl, cancellationToken);

        await _whatsApp.SendOtpAsync(mobile, otp, cancellationToken);

        _logger.LogInformation("OTP sent to {Mobile} in tenant {TenantId}", mobile, _currentTenant.TenantId);
        return Result.Success();
    }
}
