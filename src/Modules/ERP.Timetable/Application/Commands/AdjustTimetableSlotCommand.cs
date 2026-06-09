using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Common;
using ERP.Timetable.Domain;
using ERP.Timetable.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Timetable.Application.Commands;

public record AdjustTimetableSlotCommand(
    Guid EntryId,
    Guid NewTimeSlotId,
    Guid NewRoomId) : IRequest<Result>;

public class AdjustTimetableSlotHandler : IRequestHandler<AdjustTimetableSlotCommand, Result>
{
    private readonly ITimetableDbContext _db;
    private readonly ICurrentTenant _currentTenant;

    public AdjustTimetableSlotHandler(ITimetableDbContext db, ICurrentTenant currentTenant)
    {
        _db = db;
        _currentTenant = currentTenant;
    }

    public async Task<Result> Handle(AdjustTimetableSlotCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _currentTenant.TenantId!.Value;

        var entry = await _db.TimetableEntries
            .FirstOrDefaultAsync(x => x.Id == request.EntryId && x.TenantId == tenantId, cancellationToken);

        if (entry is null)
            return Result.Failure("Timetable entry not found.");

        if (entry.Status != TimetableStatus.Draft)
            return Result.Failure("Only Draft entries can be adjusted.");

        var batchConflict = await _db.TimetableEntries.AnyAsync(x =>
            x.TenantId == tenantId &&
            x.SemesterId == entry.SemesterId &&
            x.BatchId == entry.BatchId &&
            x.TimeSlotId == request.NewTimeSlotId &&
            x.Id != request.EntryId, cancellationToken);

        if (batchConflict)
            return Result.Failure("Batch already has a class in the requested time slot.");

        var facultyConflict = await _db.TimetableEntries.AnyAsync(x =>
            x.TenantId == tenantId &&
            x.SemesterId == entry.SemesterId &&
            x.FacultyUserId == entry.FacultyUserId &&
            x.TimeSlotId == request.NewTimeSlotId &&
            x.Id != request.EntryId, cancellationToken);

        if (facultyConflict)
            return Result.Failure("Faculty is already booked in the requested time slot.");

        var roomConflict = await _db.TimetableEntries.AnyAsync(x =>
            x.TenantId == tenantId &&
            x.SemesterId == entry.SemesterId &&
            x.RoomId == request.NewRoomId &&
            x.TimeSlotId == request.NewTimeSlotId &&
            x.Id != request.EntryId, cancellationToken);

        if (roomConflict)
            return Result.Failure("Room is already booked in the requested time slot.");

        entry.TimeSlotId = request.NewTimeSlotId;
        entry.RoomId = request.NewRoomId;

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
