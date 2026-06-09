using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Common;
using ERP.Timetable.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Timetable.Application.Queries;

public record TimetableGridDto(
    IReadOnlyList<TimeSlotDto> TimeSlots,
    IReadOnlyList<TimetableDayDto> Days);

public record TimeSlotDto(int PeriodNumber, TimeOnly StartTime, TimeOnly EndTime);

public record TimetableDayDto(
    string DayName,
    IReadOnlyList<TimetableCellDto> Cells);

public record TimetableCellDto(
    Guid? EntryId,
    Guid? SubjectId,
    Guid? FacultyUserId,
    string? RoomCode,
    bool IsBreak);

public record GetTimetableQuery(Guid SemesterId, Guid BatchId) : IRequest<Result<TimetableGridDto>>;

public class GetTimetableHandler : IRequestHandler<GetTimetableQuery, Result<TimetableGridDto>>
{
    private readonly ITimetableDbContext _db;
    private readonly ICurrentTenant _currentTenant;

    private static readonly string[] DayNames = { "", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };

    public GetTimetableHandler(ITimetableDbContext db, ICurrentTenant currentTenant)
    {
        _db = db;
        _currentTenant = currentTenant;
    }

    public async Task<Result<TimetableGridDto>> Handle(GetTimetableQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _currentTenant.TenantId!.Value;

        var slots = await _db.TimeSlots
            .Where(x => x.TenantId == tenantId && !x.IsDeleted)
            .OrderBy(x => x.DayOfWeek).ThenBy(x => x.PeriodNumber)
            .ToListAsync(cancellationToken);

        var entries = await _db.TimetableEntries
            .Include(x => x.Room)
            .Where(x =>
                x.TenantId == tenantId &&
                x.SemesterId == request.SemesterId &&
                x.BatchId == request.BatchId &&
                !x.IsDeleted)
            .ToListAsync(cancellationToken);

        var distinctPeriods = slots
            .Where(s => !s.IsBreak)
            .GroupBy(s => s.PeriodNumber)
            .Select(g => g.First())
            .OrderBy(s => s.PeriodNumber)
            .ToList();

        var slotDtos = distinctPeriods
            .Select(s => new TimeSlotDto(s.PeriodNumber, s.StartTime, s.EndTime))
            .ToList();

        var days = slots.Select(s => s.DayOfWeek).Distinct().OrderBy(d => d).ToList();

        var dayDtos = days.Select(day =>
        {
            var daySlots = slots.Where(s => s.DayOfWeek == day).OrderBy(s => s.PeriodNumber).ToList();
            var cells = daySlots.Select(slot =>
            {
                if (slot.IsBreak)
                    return new TimetableCellDto(null, null, null, null, true);

                var entry = entries.FirstOrDefault(e => e.TimeSlotId == slot.Id);
                return new TimetableCellDto(
                    entry?.Id,
                    entry?.SubjectId,
                    entry?.FacultyUserId,
                    entry?.Room?.Code,
                    false);
            }).ToList();

            return new TimetableDayDto(DayNames[day], cells);
        }).ToList();

        return Result.Success(new TimetableGridDto(slotDtos, dayDtos));
    }
}
