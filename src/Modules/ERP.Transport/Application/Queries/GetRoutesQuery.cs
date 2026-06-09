using ERP.Shared.Application.Common;
using ERP.Transport.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Transport.Application.Queries;

public record RouteDto(
    Guid Id,
    string Name,
    string? Description,
    Guid? VehicleId,
    Guid? DriverId,
    TimeOnly DepartureTime,
    TimeOnly ReturnTime,
    bool IsActive,
    int TotalStops,
    int TotalPassengers);

public record GetRoutesQuery(int Page = 1, int PageSize = 20) : IRequest<PagedResult<RouteDto>>;

public class GetRoutesQueryHandler : IRequestHandler<GetRoutesQuery, PagedResult<RouteDto>>
{
    private readonly ITransportDbContext _db;

    public GetRoutesQueryHandler(ITransportDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<RouteDto>> Handle(GetRoutesQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Routes.Where(r => !r.IsDeleted && r.IsActive);
        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(r => r.Name)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(r => new RouteDto(r.Id, r.Name, r.Description, r.VehicleId, r.DriverId,
                r.DepartureTime, r.ReturnTime, r.IsActive, r.TotalStops, r.TotalPassengers))
            .ToListAsync(cancellationToken);

        return new PagedResult<RouteDto>(items, total, request.Page, request.PageSize);
    }
}
