using ERP.Compliance.Application.Commands;
using ERP.Compliance.Application.Queries;
using ERP.Compliance.Domain;
using ERP.Shared.Application.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Compliance.API;

[ApiController]
[Route("api/compliance")]
[Authorize]
public class ComplianceCalendarController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentTenant _currentTenant;
    private readonly ICurrentUser _currentUser;

    public ComplianceCalendarController(IMediator mediator, ICurrentTenant currentTenant, ICurrentUser currentUser)
    {
        _mediator = mediator;
        _currentTenant = currentTenant;
        _currentUser = currentUser;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateComplianceItemRequest body, CancellationToken ct = default)
    {
        var tenantId = _currentTenant.TenantId ?? Guid.Empty;
        var result = await _mediator.Send(new CreateComplianceItemCommand(
            tenantId, body.Authority, body.Title, body.Description, body.DueDate,
            body.ResponsiblePersonId, body.ResponsiblePersonName, body.AcademicYear,
            body.IsRecurring, body.RecurrencePattern), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok(new { id = result.Value });
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] ComplianceAuthority? authority = null,
        [FromQuery] ComplianceStatus? status = null,
        [FromQuery] int? month = null,
        [FromQuery] int? academicYear = null,
        CancellationToken ct = default)
    {
        var tenantId = _currentTenant.TenantId ?? Guid.Empty;
        var result = await _mediator.Send(new GetComplianceCalendarQuery(tenantId, page, pageSize, authority, status, month, academicYear), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok(result.Value);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateComplianceItemRequest body, CancellationToken ct = default)
    {
        var tenantId = _currentTenant.TenantId ?? Guid.Empty;
        var result = await _mediator.Send(new UpdateComplianceItemCommand(
            tenantId, id, body.Authority, body.Title, body.Description, body.DueDate,
            body.ResponsiblePersonId, body.ResponsiblePersonName, body.Status,
            body.AcademicYear, body.IsRecurring, body.RecurrencePattern), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return NoContent();
    }

    [HttpPatch("{id:guid}/complete")]
    public async Task<IActionResult> MarkComplete(Guid id, [FromBody] MarkCompleteRequest body, CancellationToken ct = default)
    {
        var tenantId = _currentTenant.TenantId ?? Guid.Empty;
        var userId = _currentUser.UserId ?? Guid.Empty;
        var result = await _mediator.Send(new MarkComplianceCompleteCommand(tenantId, id, userId, body.SubmissionReference, body.Notes), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return NoContent();
    }

    [HttpGet("upcoming")]
    public async Task<IActionResult> Upcoming(CancellationToken ct = default)
    {
        var tenantId = _currentTenant.TenantId ?? Guid.Empty;
        var result = await _mediator.Send(new GetUpcomingDeadlinesQuery(tenantId), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok(result.Value);
    }

    [HttpGet("notifications")]
    public async Task<IActionResult> Notifications([FromQuery] bool unreadOnly = false, CancellationToken ct = default)
    {
        var tenantId = _currentTenant.TenantId ?? Guid.Empty;
        var userId = _currentUser.UserId ?? Guid.Empty;
        var result = await _mediator.Send(new GetComplianceNotificationsQuery(tenantId, userId, unreadOnly), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok(result.Value);
    }

    [HttpPatch("notifications/{notificationId:guid}/read")]
    public async Task<IActionResult> MarkNotificationRead(Guid notificationId, CancellationToken ct = default)
    {
        var tenantId = _currentTenant.TenantId ?? Guid.Empty;
        var userId = _currentUser.UserId ?? Guid.Empty;
        var result = await _mediator.Send(new MarkNotificationReadCommand(tenantId, notificationId, userId), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return NoContent();
    }

    [HttpPost("seed")]
    public async Task<IActionResult> Seed([FromQuery] int academicYear, CancellationToken ct = default)
    {
        var tenantId = _currentTenant.TenantId ?? Guid.Empty;
        var result = await _mediator.Send(new SeedComplianceCalendarCommand(tenantId, academicYear), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok(new { seeded = result.Value });
    }
}

public record CreateComplianceItemRequest(
    ComplianceAuthority Authority,
    string Title,
    string? Description,
    DateOnly DueDate,
    Guid? ResponsiblePersonId,
    string? ResponsiblePersonName,
    int AcademicYear,
    bool IsRecurring = false,
    string? RecurrencePattern = null);

public record UpdateComplianceItemRequest(
    ComplianceAuthority Authority,
    string Title,
    string? Description,
    DateOnly DueDate,
    Guid? ResponsiblePersonId,
    string? ResponsiblePersonName,
    ComplianceStatus Status,
    int AcademicYear,
    bool IsRecurring,
    string? RecurrencePattern);

public record MarkCompleteRequest(string? SubmissionReference, string? Notes);
