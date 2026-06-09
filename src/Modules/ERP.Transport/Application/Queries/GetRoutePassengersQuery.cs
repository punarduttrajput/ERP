using ERP.Shared.Application.Common;
using ERP.Transport.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Transport.Application.Queries;

public record RoutePassengerDto(
    Guid AssignmentId,
    Guid MemberId,
    string MemberType,
    string MemberName,
    Guid StopId,
    string StopName,
    int Sequence,
    int AcademicYear);

public record GetRoutePassengersQuery(Guid RouteId, int Page = 1, int PageSize = 50)
    : IRequest<PagedResult<RoutePassengerDto>>;

public class GetRoutePassengersQueryHandler : IRequestHandler<GetRoutePassengersQuery, PagedResult<RoutePassengerDto>>
{
    private readonly ITransportDbContext _db;

    public GetRoutePassengersQueryHandler(ITransportDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<RoutePassengerDto>> Handle(GetRoutePassengersQuery request, CancellationToken cancellationToken)
    {
        var query = from a in _db.RouteAssignments
                    join s in _db.RouteStops on a.StopId equals s.Id
                    where a.RouteId == request.RouteId && a.IsActive && !a.IsDeleted
                    select new RoutePassengerDto(a.Id, a.MemberId, a.MemberType, a.MemberName,
                        s.Id, s.Name, s.Sequence, a.AcademicYear);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(x => x.Sequence).ThenBy(x => x.MemberName)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<RoutePassengerDto>(items, total, request.Page, request.PageSize);
    }
}
