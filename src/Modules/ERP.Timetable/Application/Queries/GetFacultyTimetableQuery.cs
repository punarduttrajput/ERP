using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Common;
using ERP.Timetable.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Timetable.Application.Queries;

public record FacultyTimetableEntryDto(
    Guid EntryId,
    Guid BatchId,
    Guid SubjectId,
    Guid RoomId,
    string? RoomCode,
    int DayOfWeek,
    int PeriodNumber,
    TimeOnly StartTime,
    TimeOnly EndTime,
    bool IsSubstitute);

public record GetFacultyTimetableQuery(Guid SemesterId, Guid FacultyUserId) : IRequest<Result<IReadOnlyList<FacultyTimetableEntryDto>>>;

public class GetFacultyTimetableHandler : IRequestHandler<GetFacultyTimetableQuery, Result<IReadOnlyList<FacultyTimetableEntryDto>>>
{
    private readonly ITimetableDbContext _db;
    private readonly ICurrentTenant _currentTenant;

    public GetFacultyTimetableHandler(ITimetableDbContext db, ICurrentTenant currentTenant)
    {
        _db = db;
        _currentTenant = currentTenant;
    }

    public async Task<Result<IReadOnlyList<FacultyTimetableEntryDto>>> Handle(
        GetFacultyTimetableQuery request,
        CancellationToken cancellationToken)
    {
        var tenantId = _currentTenant.TenantId!.Value;

        var entries = await _db.TimetableEntries
            .Include(x => x.Room)
            .Include(x => x.TimeSlot)
            .Where(x =>
                x.TenantId == tenantId &&
                x.SemesterId == request.SemesterId &&
                x.FacultyUserId == request.FacultyUserId &&
                !x.IsDeleted)
            .OrderBy(x => x.TimeSlot!.DayOfWeek)
            .ThenBy(x => x.TimeSlot!.PeriodNumber)
            .ToListAsync(cancellationToken);

        var result = entries.Select(e => new FacultyTimetableEntryDto(
            e.Id,
            e.BatchId,
            e.SubjectId,
            e.RoomId,
            e.Room?.Code,
            e.TimeSlot!.DayOfWeek,
            e.TimeSlot.PeriodNumber,
            e.TimeSlot.StartTime,
            e.TimeSlot.EndTime,
            e.IsSubstitute)).ToList();

        return Result.Success<IReadOnlyList<FacultyTimetableEntryDto>>(result);
    }
}
