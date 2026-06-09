using ERP.Reporting.Application.Commands;
using ERP.Reporting.Domain;
using ERP.Reporting.Infrastructure;
using ERP.Shared.Application.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.Reporting.API;

[ApiController]
[Route("api/reports/schedules")]
[Authorize]
public class ReportSchedulesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentTenant _currentTenant;
    private readonly IReportingDbContext _db;

    public ReportSchedulesController(IMediator mediator, ICurrentTenant currentTenant, IReportingDbContext db)
    {
        _mediator = mediator;
        _currentTenant = currentTenant;
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var schedules = await _db.ReportSchedules
            .Where(x => x.TenantId == (_currentTenant.TenantId ?? Guid.Empty) && !x.IsDeleted)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
        return Ok(schedules);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateScheduleRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CreateReportScheduleCommand(
            _currentTenant.TenantId ?? Guid.Empty,
            request.ReportId,
            request.Frequency,
            request.DayOfWeek,
            request.DayOfMonth,
            request.RunAtHour,
            request.ExportFormat,
            request.Recipients), cancellationToken);
        return result.IsSuccess ? Ok(new { id = result.Value }) : BadRequest(result.Error);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateScheduleRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new UpdateReportScheduleCommand(
            id,
            _currentTenant.TenantId ?? Guid.Empty,
            request.Frequency,
            request.DayOfWeek,
            request.DayOfMonth,
            request.RunAtHour,
            request.ExportFormat,
            request.Recipients,
            request.IsActive), cancellationToken);
        return result.IsSuccess ? NoContent() : BadRequest(result.Error);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Disable(Guid id, CancellationToken cancellationToken)
    {
        var schedule = await _db.ReportSchedules
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == (_currentTenant.TenantId ?? Guid.Empty) && !x.IsDeleted, cancellationToken);

        if (schedule is null) return NotFound();

        schedule.IsActive = false;
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}

public record CreateScheduleRequest(
    Guid ReportId,
    ScheduleFrequency Frequency,
    int? DayOfWeek,
    int? DayOfMonth,
    int RunAtHour,
    ExportFormat ExportFormat,
    string[] Recipients);

public record UpdateScheduleRequest(
    ScheduleFrequency Frequency,
    int? DayOfWeek,
    int? DayOfMonth,
    int RunAtHour,
    ExportFormat ExportFormat,
    string[] Recipients,
    bool IsActive);
