using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Common;
using ERP.Timetable.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Timetable.Application.Queries;

public record RoomDto(
    Guid Id,
    string Code,
    string Name,
    int Capacity,
    string RoomType,
    string? Building,
    int? Floor,
    bool IsActive);

public record GetRoomsQuery : IRequest<Result<IReadOnlyList<RoomDto>>>;

public class GetRoomsHandler : IRequestHandler<GetRoomsQuery, Result<IReadOnlyList<RoomDto>>>
{
    private readonly ITimetableDbContext _db;
    private readonly ICurrentTenant _currentTenant;

    public GetRoomsHandler(ITimetableDbContext db, ICurrentTenant currentTenant)
    {
        _db = db;
        _currentTenant = currentTenant;
    }

    public async Task<Result<IReadOnlyList<RoomDto>>> Handle(GetRoomsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _currentTenant.TenantId!.Value;

        var rooms = await _db.Rooms
            .Where(x => x.TenantId == tenantId && !x.IsDeleted)
            .OrderBy(x => x.Code)
            .Select(x => new RoomDto(x.Id, x.Code, x.Name, x.Capacity, x.RoomType, x.Building, x.Floor, x.IsActive))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<RoomDto>>(rooms);
    }
}
