using ERP.MobileApi.Domain;
using ERP.MobileApi.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.MobileApi.Application.Commands;

public record RegisterDeviceCommand(
    Guid TenantId,
    Guid UserId,
    string DeviceToken,
    DevicePlatform Platform,
    string? AppVersion
) : IRequest<Result<Guid>>;

public class RegisterDeviceHandler : IRequestHandler<RegisterDeviceCommand, Result<Guid>>
{
    private readonly IMobileDbContext _db;

    public RegisterDeviceHandler(IMobileDbContext db) => _db = db;

    public async Task<Result<Guid>> Handle(RegisterDeviceCommand request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var existing = await _db.DeviceRegistrations
            .FirstOrDefaultAsync(x => x.TenantId == request.TenantId
                && x.UserId == request.UserId
                && x.DeviceToken == request.DeviceToken, cancellationToken);

        if (existing is not null)
        {
            existing.IsActive = true;
            existing.LastSeenAt = now;
            existing.AppVersion = request.AppVersion;
            await _db.SaveChangesAsync(cancellationToken);
            return Result<Guid>.Success(existing.Id);
        }

        var registration = new DeviceRegistration
        {
            TenantId = request.TenantId,
            UserId = request.UserId,
            DeviceToken = request.DeviceToken,
            Platform = request.Platform,
            AppVersion = request.AppVersion,
            IsActive = true,
            RegisteredAt = now,
            LastSeenAt = now
        };

        await _db.DeviceRegistrations.AddAsync(registration, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(registration.Id);
    }
}
