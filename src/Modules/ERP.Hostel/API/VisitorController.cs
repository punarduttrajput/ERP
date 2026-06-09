using ERP.Hostel.Application.Commands;
using ERP.Hostel.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Hostel.API;

[ApiController]
[Route("api/hostel/visitors")]
[Authorize]
public class VisitorController : ControllerBase
{
    private readonly IMediator _mediator;

    public VisitorController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("check-in")]
    public async Task<IActionResult> CheckIn([FromBody] CheckInVisitorRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CheckInVisitorCommand(
            request.VisitorName,
            request.VisitorMobile,
            request.VisitorIdType,
            request.VisitorIdNumber,
            request.StudentId,
            request.StudentName,
            request.BlockId,
            request.PurposeOfVisit,
            request.CheckedInBy), ct);

        if (!result.IsSuccess) return BadRequest(result.Error);
        return Ok(new { id = result.Value });
    }

    [HttpPost("{id:guid}/check-out")]
    public async Task<IActionResult> CheckOut(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new CheckOutVisitorCommand(id), ct);
        if (!result.IsSuccess) return BadRequest(result.Error);
        return Ok();
    }

    [HttpGet]
    public async Task<IActionResult> GetLog([FromQuery] Guid? blockId, [FromQuery] DateOnly? date, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetVisitorLogQuery(blockId, date), ct);
        return Ok(result.Value);
    }
}

public record CheckInVisitorRequest(
    string VisitorName,
    string VisitorMobile,
    string VisitorIdType,
    string VisitorIdNumber,
    Guid StudentId,
    string StudentName,
    Guid BlockId,
    string PurposeOfVisit,
    Guid CheckedInBy
);
