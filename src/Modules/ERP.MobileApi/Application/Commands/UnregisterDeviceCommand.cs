using ERP.MobileApi.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.MobileApi.Application.Commands;

public record UnregisterDeviceCommand(
    Guid TenantId,
    Guid UserId,
    string DeviceToken
) : IRequest<Result>;

public class UnregisterDeviceHandler : IRequestHandler<UnregisterDeviceCommand, Result>
{
    private readonly IMobileDbContext _db;

    public UnregisterDeviceHandler(IMobileDbContext db) => _db = db;

    public async Task<Result> Handle(UnregisterDeviceCommand request, CancellationToken cancellationToken)
    {
        var registration = await _db.DeviceRegistrations
            .FirstOrDefaultAsync(x => x.TenantId == request.TenantId
                && x.UserId == request.UserId
                && x.DeviceToken == request.DeviceToken, cancellationToken);

        if (registration is null)
            return Result.Failure("Device registration not found.");

        registration.IsActive = false;
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
