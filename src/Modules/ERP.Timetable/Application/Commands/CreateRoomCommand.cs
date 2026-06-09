using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Common;
using ERP.Timetable.Domain;
using ERP.Timetable.Infrastructure;
using MediatR;

namespace ERP.Timetable.Application.Commands;

public record CreateRoomCommand(
    string Code,
    string Name,
    int Capacity,
    string RoomType,
    string? Building,
    int? Floor) : IRequest<Result<Guid>>;

public class CreateRoomHandler : IRequestHandler<CreateRoomCommand, Result<Guid>>
{
    private readonly ITimetableDbContext _db;
    private readonly ICurrentTenant _currentTenant;

    public CreateRoomHandler(ITimetableDbContext db, ICurrentTenant currentTenant)
    {
        _db = db;
        _currentTenant = currentTenant;
    }

    public async Task<Result<Guid>> Handle(CreateRoomCommand request, CancellationToken cancellationToken)
    {
        var room = new Room
        {
            TenantId = _currentTenant.TenantId!.Value,
            Code = request.Code,
            Name = request.Name,
            Capacity = request.Capacity,
            RoomType = request.RoomType,
            Building = request.Building,
            Floor = request.Floor
        };

        await _db.Rooms.AddAsync(room, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success(room.Id);
    }
}
