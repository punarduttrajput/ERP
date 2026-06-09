using ERP.Hostel.Domain;
using ERP.Hostel.Infrastructure;
using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Hostel.Application.Commands;

public record AllocateRoomCommand(
    Guid RoomId,
    Guid StudentId,
    string StudentName,
    int AcademicYear
) : IRequest<Result<AllocateRoomResult>>;

public record AllocateRoomResult(Guid? AllocationId, string Message, bool IsWaitlisted);

public class AllocateRoomCommandHandler : IRequestHandler<AllocateRoomCommand, Result<AllocateRoomResult>>
{
    private readonly IHostelDbContext _db;
    private readonly ICurrentTenant _currentTenant;

    public AllocateRoomCommandHandler(IHostelDbContext db, ICurrentTenant currentTenant)
    {
        _db = db;
        _currentTenant = currentTenant;
    }

    public async Task<Result<AllocateRoomResult>> Handle(AllocateRoomCommand request, CancellationToken cancellationToken)
    {
        var room = await _db.HostelRooms
            .Include(r => r.Block)
            .FirstOrDefaultAsync(r => r.Id == request.RoomId, cancellationToken);

        if (room is null)
            return Result<AllocateRoomResult>.Failure("Room not found.");

        if (room.OccupiedCount >= room.Capacity)
        {
            var maxPriority = await _db.HostelWaitlist
                .Where(w => !w.IsPromoted)
                .Select(w => (int?)w.Priority)
                .MaxAsync(cancellationToken) ?? 0;

            var entry = new WaitlistEntry
            {
                TenantId = _currentTenant.TenantId ?? Guid.Empty,
                StudentId = request.StudentId,
                StudentName = request.StudentName,
                PreferredRoomType = room.RoomType,
                PreferredBlockId = room.BlockId,
                AcademicYear = request.AcademicYear,
                RequestedAt = DateTime.UtcNow,
                Priority = maxPriority + 1
            };

            _db.HostelWaitlist.Add(entry);
            await _db.SaveChangesAsync(cancellationToken);

            return Result<AllocateRoomResult>.Success(new AllocateRoomResult(
                null,
                $"Room full — added to waitlist at position {entry.Priority}",
                true));
        }

        var wasFullyOccupied = room.OccupiedCount == room.Capacity;

        var allocation = new RoomAllocation
        {
            TenantId = _currentTenant.TenantId ?? Guid.Empty,
            RoomId = request.RoomId,
            StudentId = request.StudentId,
            StudentName = request.StudentName,
            AcademicYear = request.AcademicYear,
            AllocatedAt = DateTime.UtcNow,
            Status = AllocationStatus.Active
        };

        _db.RoomAllocations.Add(allocation);

        room.OccupiedCount += 1;
        room.Status = ComputeStatus(room.OccupiedCount, room.Capacity);

        // Track transition to FullyOccupied so OccupiedRooms on block stays accurate
        if (room.Status == RoomStatus.FullyOccupied && room.Block is not null)
            room.Block.OccupiedRooms += 1;

        await _db.SaveChangesAsync(cancellationToken);

        return Result<AllocateRoomResult>.Success(new AllocateRoomResult(
            allocation.Id,
            $"Allocated to room {room.RoomNumber}",
            false));
    }

    internal static RoomStatus ComputeStatus(int occupiedCount, int capacity)
    {
        if (occupiedCount == 0) return RoomStatus.Available;
        if (occupiedCount >= capacity) return RoomStatus.FullyOccupied;
        return RoomStatus.PartiallyOccupied;
    }
}
