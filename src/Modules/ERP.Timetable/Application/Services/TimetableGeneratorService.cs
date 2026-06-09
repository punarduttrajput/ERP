using ERP.Shared.Application.Common;
using ERP.Timetable.Domain;
using ERP.Timetable.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ERP.Timetable.Application.Services;

public class TimetableGeneratorService
{
    private readonly ITimetableDbContext _db;

    public TimetableGeneratorService(ITimetableDbContext db)
    {
        _db = db;
    }

    public async Task<Result<int>> GenerateAsync(
        Guid semesterId,
        Guid batchId,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var assignments = await _db.FacultySubjectAssignments
            .Where(x => x.SemesterId == semesterId && x.BatchId == batchId && x.TenantId == tenantId && !x.IsDeleted)
            .ToListAsync(cancellationToken);

        var slots = await _db.TimeSlots
            .Where(x => x.TenantId == tenantId && !x.IsBreak && !x.IsDeleted)
            .OrderBy(x => x.DayOfWeek).ThenBy(x => x.PeriodNumber)
            .ToListAsync(cancellationToken);

        var existingEntries = await _db.TimetableEntries
            .Where(x => x.SemesterId == semesterId && x.TenantId == tenantId && !x.IsDeleted)
            .ToListAsync(cancellationToken);

        var workloads = await _db.FacultyWorkloads
            .Where(x => x.SemesterId == semesterId && x.TenantId == tenantId && !x.IsDeleted)
            .ToListAsync(cancellationToken);

        var rooms = await _db.Rooms
            .Where(x => x.TenantId == tenantId && x.IsActive && !x.IsDeleted)
            .ToListAsync(cancellationToken);

        if (rooms.Count == 0)
            return Result.Failure<int>("No active rooms available for scheduling.");

        // In-memory tracking sets to enforce the three unique-index constraints
        // Key: (semesterId, batchId, slotId)
        var batchSlotUsed = new HashSet<(Guid, Guid, Guid)>(
            existingEntries.Select(e => (e.SemesterId, e.BatchId, e.TimeSlotId)));

        // Key: (semesterId, facultyId, slotId)
        var facultySlotUsed = new HashSet<(Guid, Guid, Guid)>(
            existingEntries.Select(e => (e.SemesterId, e.FacultyUserId, e.TimeSlotId)));

        // Key: (semesterId, roomId, slotId)
        var roomSlotUsed = new HashSet<(Guid, Guid, Guid)>(
            existingEntries.Select(e => (e.SemesterId, e.RoomId, e.TimeSlotId)));

        // Track workload increments in memory before persisting
        var workloadDelta = new Dictionary<Guid, int>();

        var newEntries = new List<TimetableEntry>();

        foreach (var assignment in assignments)
        {
            var workload = workloads.FirstOrDefault(w => w.FacultyUserId == assignment.FacultyUserId);
            int currentAssigned = (workload?.AssignedHoursPerWeek ?? 0)
                + workloadDelta.GetValueOrDefault(assignment.FacultyUserId, 0);
            int maxAllowed = workload?.MaxHoursPerWeek ?? int.MaxValue;

            int placed = 0;
            foreach (var slot in slots)
            {
                if (placed >= assignment.HoursPerWeek)
                    break;

                if (currentAssigned >= maxAllowed)
                    break;

                var batchKey = (semesterId, batchId, slot.Id);
                var facultyKey = (semesterId, assignment.FacultyUserId, slot.Id);

                if (batchSlotUsed.Contains(batchKey))
                    continue;
                if (facultySlotUsed.Contains(facultyKey))
                    continue;

                var room = rooms.FirstOrDefault(r =>
                    !roomSlotUsed.Contains((semesterId, r.Id, slot.Id)));

                if (room is null)
                    continue;

                var entry = new TimetableEntry
                {
                    TenantId = tenantId,
                    SemesterId = semesterId,
                    BatchId = batchId,
                    SubjectId = assignment.SubjectId,
                    FacultyUserId = assignment.FacultyUserId,
                    RoomId = room.Id,
                    TimeSlotId = slot.Id,
                    Status = TimetableStatus.Draft,
                    IsSubstitute = false
                };

                newEntries.Add(entry);
                batchSlotUsed.Add(batchKey);
                facultySlotUsed.Add(facultyKey);
                roomSlotUsed.Add((semesterId, room.Id, slot.Id));

                placed++;
                currentAssigned++;
                workloadDelta[assignment.FacultyUserId] =
                    workloadDelta.GetValueOrDefault(assignment.FacultyUserId, 0) + 1;
            }

            if (placed < assignment.HoursPerWeek)
            {
                return Result.Failure<int>(
                    $"Could not schedule all {assignment.HoursPerWeek} periods for SubjectId={assignment.SubjectId} " +
                    $"(FacultyUserId={assignment.FacultyUserId}). Only {placed} slot(s) found.");
            }
        }

        await _db.TimetableEntries.AddRangeAsync(newEntries, cancellationToken);

        // Update AssignedHoursPerWeek on workloads that exist
        foreach (var (facultyId, delta) in workloadDelta)
        {
            var workload = workloads.FirstOrDefault(w => w.FacultyUserId == facultyId);
            if (workload is not null)
                workload.AssignedHoursPerWeek += delta;
        }

        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(newEntries.Count);
    }
}
