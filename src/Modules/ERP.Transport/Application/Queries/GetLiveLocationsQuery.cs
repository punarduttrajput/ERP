using ERP.Transport.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Transport.Application.Queries;

public record LiveLocationDto(
    Guid VehicleId,
    string RegistrationNumber,
    decimal Latitude,
    decimal Longitude,
    decimal? Speed,
    decimal? Heading,
    DateTime RecordedAt);

public record GetLiveLocationsQuery : IRequest<IReadOnlyList<LiveLocationDto>>;

public class GetLiveLocationsQueryHandler : IRequestHandler<GetLiveLocationsQuery, IReadOnlyList<LiveLocationDto>>
{
    private readonly ITransportDbContext _db;

    public GetLiveLocationsQueryHandler(ITransportDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<LiveLocationDto>> Handle(GetLiveLocationsQuery request, CancellationToken cancellationToken)
    {
        // Simulate DISTINCT ON (VehicleId) by grouping on VehicleId and picking the latest RecordedAt per group.
        // EF Core translates this to a correlated subquery on MySQL which is equivalent to LATERAL JOIN / DISTINCT ON.
        var latestPerVehicle = await (
            from loc in _db.GpsLocations
            where !loc.IsDeleted
            group loc by loc.VehicleId into g
            select new
            {
                VehicleId = g.Key,
                MaxRecordedAt = g.Max(x => x.RecordedAt)
            }
        ).ToListAsync(cancellationToken);

        var vehicleIds = latestPerVehicle.Select(x => x.VehicleId).ToList();

        var locations = await _db.GpsLocations
            .Where(l => vehicleIds.Contains(l.VehicleId) && !l.IsDeleted)
            .ToListAsync(cancellationToken);

        var vehicles = await _db.Vehicles
            .Where(v => vehicleIds.Contains(v.Id) && !v.IsDeleted)
            .Select(v => new { v.Id, v.RegistrationNumber })
            .ToListAsync(cancellationToken);

        var regMap = vehicles.ToDictionary(v => v.Id, v => v.RegistrationNumber);

        var result = latestPerVehicle
            .Select(lv =>
            {
                var loc = locations
                    .Where(l => l.VehicleId == lv.VehicleId && l.RecordedAt == lv.MaxRecordedAt)
                    .OrderByDescending(l => l.RecordedAt)
                    .First();

                regMap.TryGetValue(lv.VehicleId, out var reg);
                return new LiveLocationDto(
                    lv.VehicleId,
                    reg ?? string.Empty,
                    loc.Latitude,
                    loc.Longitude,
                    loc.Speed,
                    loc.Heading,
                    loc.RecordedAt);
            })
            .ToList();

        return result;
    }
}
