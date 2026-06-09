using ERP.Timetable.Application.Commands;
using ERP.Timetable.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Timetable.API;

[Authorize]
[ApiController]
[Route("api/timetable")]
public class TimetableController : ControllerBase
{
    private readonly IMediator _mediator;

    public TimetableController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("generate")]
    public async Task<IActionResult> Generate([FromBody] GenerateTimetableCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(new { entriesCreated = result.Value });
    }

    [HttpGet]
    public async Task<IActionResult> GetTimetable([FromQuery] Guid semesterId, [FromQuery] Guid batchId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetTimetableQuery(semesterId, batchId), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(result.Value);
    }

    [HttpGet("faculty")]
    public async Task<IActionResult> GetFacultyTimetable([FromQuery] Guid semesterId, [FromQuery] Guid facultyUserId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetFacultyTimetableQuery(semesterId, facultyUserId), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(result.Value);
    }

    [HttpPatch("{entryId:guid}/slot")]
    public async Task<IActionResult> AdjustSlot(Guid entryId, [FromBody] AdjustSlotRequest req, CancellationToken ct)
    {
        var result = await _mediator.Send(new AdjustTimetableSlotCommand(entryId, req.NewTimeSlotId, req.NewRoomId), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return NoContent();
    }

    [HttpPost("{semesterId:guid}/{batchId:guid}/publish")]
    public async Task<IActionResult> Publish(Guid semesterId, Guid batchId, CancellationToken ct)
    {
        var result = await _mediator.Send(new PublishTimetableCommand(semesterId, batchId), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return NoContent();
    }

    [HttpPost("substitute")]
    public async Task<IActionResult> AssignSubstitute([FromBody] AssignSubstituteCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(new { id = result.Value });
    }
}

public record AdjustSlotRequest(Guid NewTimeSlotId, Guid NewRoomId);
