using System.Text.Json;
using ERP.Reporting.Domain;
using ERP.Reporting.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;

namespace ERP.Reporting.Application.Commands;

public record CreateReportScheduleCommand(
    Guid TenantId,
    Guid ReportId,
    ScheduleFrequency Frequency,
    int? DayOfWeek,
    int? DayOfMonth,
    int RunAtHour,
    ExportFormat ExportFormat,
    string[] Recipients) : IRequest<Result<Guid>>;

public class CreateReportScheduleHandler : IRequestHandler<CreateReportScheduleCommand, Result<Guid>>
{
    private readonly IReportingDbContext _db;

    public CreateReportScheduleHandler(IReportingDbContext db) => _db = db;

    public async Task<Result<Guid>> Handle(CreateReportScheduleCommand request, CancellationToken cancellationToken)
    {
        var schedule = new ReportSchedule
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            ReportId = request.ReportId,
            Frequency = request.Frequency,
            DayOfWeek = request.DayOfWeek,
            DayOfMonth = request.DayOfMonth,
            RunAtHour = request.RunAtHour,
            ExportFormat = request.ExportFormat,
            Recipients = JsonSerializer.Serialize(request.Recipients),
            IsActive = true,
            NextRunAt = ComputeNextRunAt(request.Frequency, request.DayOfWeek, request.DayOfMonth, request.RunAtHour, DateTime.UtcNow)
        };

        _db.ReportSchedules.Add(schedule);
        await _db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(schedule.Id);
    }

    public static DateTime ComputeNextRunAt(ScheduleFrequency frequency, int? dayOfWeek, int? dayOfMonth, int runAtHour, DateTime from)
    {
        var base_ = from.Date.AddHours(runAtHour);
        if (base_ <= from) base_ = base_.AddDays(1);

        return frequency switch
        {
            ScheduleFrequency.Daily => base_,
            ScheduleFrequency.Weekly => FindNextWeekday(base_, dayOfWeek ?? 1),
            ScheduleFrequency.Monthly => FindNextMonthDay(base_, dayOfMonth ?? 1),
            ScheduleFrequency.Quarterly => FindNextQuarterDay(base_, dayOfMonth ?? 1),
            _ => base_
        };
    }

    private static DateTime FindNextWeekday(DateTime from, int targetDow)
    {
        // targetDow: 1=Monday ... 7=Sunday (ISO 8601)
        var dow = (int)from.DayOfWeek;
        if (dow == 0) dow = 7;
        var diff = targetDow - dow;
        if (diff <= 0) diff += 7;
        return from.AddDays(diff);
    }

    private static DateTime FindNextMonthDay(DateTime from, int day)
    {
        var candidate = new DateTime(from.Year, from.Month, 1).AddHours(from.Hour);
        var maxDay = DateTime.DaysInMonth(candidate.Year, candidate.Month);
        candidate = candidate.AddDays(Math.Min(day, maxDay) - 1);
        if (candidate <= from)
        {
            candidate = candidate.AddMonths(1);
            maxDay = DateTime.DaysInMonth(candidate.Year, candidate.Month);
            candidate = new DateTime(candidate.Year, candidate.Month, Math.Min(day, maxDay)).AddHours(from.Hour);
        }
        return candidate;
    }

    private static DateTime FindNextQuarterDay(DateTime from, int day)
    {
        var result = FindNextMonthDay(from, day);
        // Advance by 3-month increments until past 'from'
        while (result <= from)
            result = FindNextMonthDay(result.AddMonths(3), day);
        return result;
    }
}
