using System.Text.Json;
using ERP.Reporting.Domain;
using ERP.Reporting.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Reporting.Application.Commands;

public record UpdateReportScheduleCommand(
    Guid Id,
    Guid TenantId,
    ScheduleFrequency Frequency,
    int? DayOfWeek,
    int? DayOfMonth,
    int RunAtHour,
    ExportFormat ExportFormat,
    string[] Recipients,
    bool IsActive) : IRequest<Result>;

public class UpdateReportScheduleHandler : IRequestHandler<UpdateReportScheduleCommand, Result>
{
    private readonly IReportingDbContext _db;

    public UpdateReportScheduleHandler(IReportingDbContext db) => _db = db;

    public async Task<Result> Handle(UpdateReportScheduleCommand request, CancellationToken cancellationToken)
    {
        var schedule = await _db.ReportSchedules
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.TenantId == request.TenantId && !x.IsDeleted, cancellationToken);

        if (schedule is null)
            return Result.Failure("Report schedule not found.");

        schedule.Frequency = request.Frequency;
        schedule.DayOfWeek = request.DayOfWeek;
        schedule.DayOfMonth = request.DayOfMonth;
        schedule.RunAtHour = request.RunAtHour;
        schedule.ExportFormat = request.ExportFormat;
        schedule.Recipients = JsonSerializer.Serialize(request.Recipients);
        schedule.IsActive = request.IsActive;
        schedule.NextRunAt = CreateReportScheduleHandler.ComputeNextRunAt(
            request.Frequency, request.DayOfWeek, request.DayOfMonth, request.RunAtHour, DateTime.UtcNow);

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
