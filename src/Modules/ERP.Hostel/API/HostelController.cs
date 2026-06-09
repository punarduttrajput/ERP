using ERP.Hostel.Application.Commands;
using ERP.Hostel.Application.Queries;
using ERP.Hostel.Domain;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Hostel.API;

[ApiController]
[Route("api/hostel")]
[Authorize]
public class HostelController : ControllerBase
{
    private readonly IMediator _mediator;

    public HostelController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("blocks")]
    public async Task<IActionResult> CreateBlock([FromBody] CreateBlockRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateBlockCommand(request.Name, request.Gender), ct);
        if (!result.IsSuccess) return BadRequest(result.Error);
        return Ok(new { id = result.Value });
    }

    [HttpGet("blocks")]
    public async Task<IActionResult> GetBlocks(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetBlocksQuery(), ct);
        return Ok(result.Value);
    }

    [HttpPost("blocks/{blockId:guid}/rooms")]
    public async Task<IActionResult> CreateRoom(Guid blockId, [FromBody] CreateRoomRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateRoomCommand(
            blockId, request.RoomNumber, request.Floor, request.RoomType, request.Capacity, request.MonthlyRent), ct);
        if (!result.IsSuccess) return BadRequest(result.Error);
        return Ok(new { id = result.Value });
    }

    [HttpGet("rooms")]
    public async Task<IActionResult> GetRooms([FromQuery] Guid? blockId, [FromQuery] RoomStatus? status, [FromQuery] RoomType? type, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetRoomsQuery(blockId, status, type), ct);
        return Ok(result.Value);
    }

    [HttpPost("rooms/{roomId:guid}/allocate")]
    public async Task<IActionResult> AllocateRoom(Guid roomId, [FromBody] AllocateRoomRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new AllocateRoomCommand(roomId, request.StudentId, request.StudentName, request.AcademicYear), ct);
        if (!result.IsSuccess) return BadRequest(result.Error);
        return Ok(result.Value);
    }

    [HttpPost("allocations/{allocationId:guid}/vacate")]
    public async Task<IActionResult> VacateRoom(Guid allocationId, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeallocateRoomCommand(allocationId), ct);
        if (!result.IsSuccess) return BadRequest(result.Error);
        return Ok();
    }

    [HttpGet("students/{studentId:guid}/allocation")]
    public async Task<IActionResult> GetStudentAllocation(Guid studentId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetAllocationQuery(studentId), ct);
        if (result.Value is null) return NotFound();
        return Ok(result.Value);
    }

    [HttpGet("waitlist")]
    public async Task<IActionResult> GetWaitlist([FromQuery] Guid? blockId, [FromQuery] RoomType? roomType, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetWaitlistQuery(blockId, roomType), ct);
        return Ok(result.Value);
    }
}

public record CreateBlockRequest(string Name, string Gender);
public record CreateRoomRequest(string RoomNumber, int Floor, RoomType RoomType, int Capacity, decimal MonthlyRent);
public record AllocateRoomRequest(Guid StudentId, string StudentName, int AcademicYear);
