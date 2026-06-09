using ERP.Transport.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Transport.Application.Queries;

public record RouteStopDto(
    Guid Id,
    Guid RouteId,
    string Name,
    int Sequence,
    TimeOnly? PickupTime,
    decimal? DistanceFromCollegeKm);

public record GetRouteStopsQuery(Guid RouteId) : IRequest<IReadOnlyList<RouteStopDto>>;

public class GetRouteStopsQueryHandler : IRequestHandler<GetRouteStopsQuery, IReadOnlyList<RouteStopDto>>
{
    private readonly ITransportDbContext _db;

    public GetRouteStopsQueryHandler(ITransportDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<RouteStopDto>> Handle(GetRouteStopsQuery request, CancellationToken cancellationToken)
    {
        return await _db.RouteStops
            .Where(s => s.RouteId == request.RouteId && !s.IsDeleted)
            .OrderBy(s => s.Sequence)
            .Select(s => new RouteStopDto(s.Id, s.RouteId, s.Name, s.Sequence, s.PickupTime, s.DistanceFromCollegeKm))
            .ToListAsync(cancellationToken);
    }
}
