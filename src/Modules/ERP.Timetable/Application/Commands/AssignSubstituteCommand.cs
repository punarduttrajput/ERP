using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Common;
using ERP.Timetable.Domain;
using ERP.Timetable.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Timetable.Application.Commands;

public record AssignSubstituteCommand(
    Guid OriginalEntryId,
    Guid SubstituteFacultyUserId,
    DateOnly Date,
    string? Reason) : IRequest<Result<Guid>>;

public class AssignSubstituteHandler : IRequestHandler<AssignSubstituteCommand, Result<Guid>>
{
    private readonly ITimetableDbContext _db;
    private readonly ICurrentTenant _currentTenant;

    public AssignSubstituteHandler(ITimetableDbContext db, ICurrentTenant currentTenant)
    {
        _db = db;
        _currentTenant = currentTenant;
    }

    public async Task<Result<Guid>> Handle(AssignSubstituteCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _currentTenant.TenantId!.Value;

        var original = await _db.TimetableEntries
            .FirstOrDefaultAsync(x => x.Id == request.OriginalEntryId && x.TenantId == tenantId, cancellationToken);

        if (original is null)
            return Result.Failure<Guid>("Original timetable entry not found.");

        if (original.Status != TimetableStatus.Published)
            return Result.Failure<Guid>("Substitute can only be assigned to a Published entry.");

        // Check substitute not already booked for same slot in this semester
        var substituteConflict = await _db.TimetableEntries.AnyAsync(x =>
            x.TenantId == tenantId &&
            x.SemesterId == original.SemesterId &&
            x.FacultyUserId == request.SubstituteFacultyUserId &&
            x.TimeSlotId == original.TimeSlotId &&
            !x.IsDeleted,
            cancellationToken);

        if (substituteConflict)
            return Result.Failure<Guid>("Substitute faculty is already booked in this time slot.");

        var sub = new SubstituteAssignment
        {
            TenantId = tenantId,
            OriginalEntryId = request.OriginalEntryId,
            SubstituteFacultyUserId = request.SubstituteFacultyUserId,
            Date = request.Date,
            Reason = request.Reason
        };

        var substituteEntry = new TimetableEntry
        {
            TenantId = tenantId,
            SemesterId = original.SemesterId,
            BatchId = original.BatchId,
            SubjectId = original.SubjectId,
            FacultyUserId = request.SubstituteFacultyUserId,
            RoomId = original.RoomId,
            TimeSlotId = original.TimeSlotId,
            Status = TimetableStatus.Published,
            IsSubstitute = true,
            SubstituteForEntryId = original.Id
        };

        await _db.SubstituteAssignments.AddAsync(sub, cancellationToken);
        await _db.TimetableEntries.AddAsync(substituteEntry, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(sub.Id);
    }
}
