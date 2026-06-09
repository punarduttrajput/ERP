using ERP.Timetable.Application.Commands;
using ERP.Timetable.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Timetable.API;

[Authorize]
[ApiController]
[Route("api/rooms")]
public class RoomsController : ControllerBase
{
    private readonly IMediator _mediator;

    public RoomsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> CreateRoom([FromBody] CreateRoomCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(new { id = result.Value });
    }

    [HttpGet]
    public async Task<IActionResult> GetRooms(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetRoomsQuery(), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(result.Value);
    }
}
