using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Common;
using ERP.Transport.Domain;
using ERP.Transport.Infrastructure;
using MediatR;

namespace ERP.Transport.Application.Commands;

public record UpdateGpsLocationCommand(
    Guid VehicleId,
    decimal Latitude,
    decimal Longitude,
    decimal? Speed,
    decimal? Heading,
    DateTime RecordedAt,
    string? ProviderReference
) : IRequest<Result>;

public class UpdateGpsLocationCommandHandler : IRequestHandler<UpdateGpsLocationCommand, Result>
{
    private readonly ITransportDbContext _db;
    private readonly ICurrentTenant _currentTenant;

    public UpdateGpsLocationCommandHandler(ITransportDbContext db, ICurrentTenant currentTenant)
    {
        _db = db;
        _currentTenant = currentTenant;
    }

    public async Task<Result> Handle(UpdateGpsLocationCommand request, CancellationToken cancellationToken)
    {
        // Always insert — never upsert. Full history enables replay and analytics.
        var location = new GpsLocation
        {
            TenantId = _currentTenant.TenantId ?? Guid.Empty,
            VehicleId = request.VehicleId,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            Speed = request.Speed,
            Heading = request.Heading,
            RecordedAt = request.RecordedAt,
            ProviderReference = request.ProviderReference
        };

        _db.GpsLocations.Add(location);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
