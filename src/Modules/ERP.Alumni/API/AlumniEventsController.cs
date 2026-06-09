using ERP.Alumni.Application.Commands;
using ERP.Alumni.Application.Queries;
using ERP.Alumni.Domain;
using ERP.Shared.Application.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Alumni.API;

[ApiController]
[Route("api/alumni/events")]
[Authorize]
public class AlumniEventsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentTenant _currentTenant;

    public AlumniEventsController(IMediator mediator, ICurrentTenant currentTenant)
    {
        _mediator = mediator;
        _currentTenant = currentTenant;
    }

    private Guid TenantId => _currentTenant.TenantId ?? Guid.Empty;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateEventRequest request, CancellationToken ct)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var organisedBy = userId is not null ? Guid.Parse(userId) : Guid.Empty;

        var result = await _mediator.Send(new CreateEventCommand(
            TenantId, organisedBy, request.Title, request.Description,
            request.EventType, request.EventDate, request.EventTime,
            request.VenueOrLink, request.MaxParticipants), ct);

        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok(new { eventId = result.Value });
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] EventType? eventType,
        [FromQuery] bool? upcomingOnly,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetEventsQuery(TenantId, eventType, upcomingOnly), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok(result.Value);
    }

    [HttpPost("{id:guid}/register")]
    public async Task<IActionResult> RegisterForEvent(Guid id, [FromBody] RegisterForEventRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new RegisterForEventCommand(TenantId, id, request.AlumniId), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok(new { registrationId = result.Value });
    }

    [HttpPatch("{id:guid}/attend/{alumniId:guid}")]
    public async Task<IActionResult> MarkAttendance(Guid id, Guid alumniId, CancellationToken ct)
    {
        var result = await _mediator.Send(new MarkAttendanceCommand(TenantId, id, alumniId), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return NoContent();
    }

    [HttpPost("{id:guid}/publish")]
    public async Task<IActionResult> Publish(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new PublishEventCommand(TenantId, id), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return NoContent();
    }
}

public record CreateEventRequest(
    string Title,
    string? Description,
    EventType EventType,
    DateOnly EventDate,
    TimeOnly? EventTime,
    string? VenueOrLink,
    int? MaxParticipants
);

public record RegisterForEventRequest(Guid AlumniId);
