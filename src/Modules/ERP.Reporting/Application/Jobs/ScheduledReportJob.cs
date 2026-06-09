using System.Text.Json;
using ERP.Reporting.Application.Commands;
using ERP.Reporting.Domain;
using ERP.Reporting.Infrastructure;
using ERP.Shared.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERP.Reporting.Application.Jobs;

public class ScheduledReportJob
{
    private readonly IReportingDbContext _db;
    private readonly IMediator _mediator;
    private readonly IEmailService _emailService;
    private readonly ILogger<ScheduledReportJob> _logger;

    public ScheduledReportJob(
        IReportingDbContext db,
        IMediator mediator,
        IEmailService emailService,
        ILogger<ScheduledReportJob> logger)
    {
        _db = db;
        _mediator = mediator;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var dueSchedules = await _db.ReportSchedules
            .Where(s => s.IsActive && !s.IsDeleted && s.NextRunAt <= now)
            .ToListAsync(cancellationToken);

        foreach (var schedule in dueSchedules)
        {
            try
            {
                var exportResult = await _mediator.Send(new ExportReportCommand(
                    schedule.TenantId,
                    schedule.ReportId == Guid.Empty ? null : schedule.ReportId,
                    null,
                    null,
                    schedule.ExportFormat,
                    null,
                    null), cancellationToken);

                if (!exportResult.IsSuccess)
                {
                    _logger.LogWarning("Scheduled report {ScheduleId} failed: {Error}", schedule.Id, exportResult.Error);
                    continue;
                }

                var export = exportResult.Value!;
                var recipients = JsonSerializer.Deserialize<string[]>(schedule.Recipients) ?? Array.Empty<string>();

                foreach (var recipient in recipients)
                {
                    try
                    {
                        // Attachments are not directly supported via IEmailService — send notification with download link.
                        // In production, upload the file to blob storage and include the link.
                        await _emailService.SendAsync(
                            recipient,
                            $"Scheduled Report: {export.FileName}",
                            $"<p>Your scheduled report is ready: <strong>{export.FileName}</strong></p>" +
                            $"<p>Rows exported: {exportResult.Value!.Content.Length} bytes.</p>",
                            cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to email report to {Recipient}", recipient);
                    }
                }

                schedule.LastRunAt = now;
                schedule.NextRunAt = ComputeNextRunAt(schedule);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running scheduled report {ScheduleId}", schedule.Id);
            }
        }

        if (dueSchedules.Any())
            await _db.SaveChangesAsync(cancellationToken);
    }

    private static DateTime ComputeNextRunAt(ReportSchedule schedule)
    {
        var from = schedule.LastRunAt ?? DateTime.UtcNow;
        return schedule.Frequency switch
        {
            ScheduleFrequency.Daily => from.AddDays(1).Date.AddHours(schedule.RunAtHour),
            ScheduleFrequency.Weekly => from.AddDays(7).Date.AddHours(schedule.RunAtHour),
            ScheduleFrequency.Monthly => ClampMonthDay(from.AddMonths(1), schedule.DayOfMonth ?? 1, schedule.RunAtHour),
            ScheduleFrequency.Quarterly => ClampMonthDay(from.AddMonths(3), schedule.DayOfMonth ?? 1, schedule.RunAtHour),
            _ => from.AddDays(1).Date.AddHours(schedule.RunAtHour)
        };
    }

    private static DateTime ClampMonthDay(DateTime dt, int day, int hour)
    {
        var maxDay = DateTime.DaysInMonth(dt.Year, dt.Month);
        return new DateTime(dt.Year, dt.Month, Math.Min(day, maxDay)).AddHours(hour);
    }
}
