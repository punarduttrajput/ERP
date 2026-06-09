using ERP.Transport.Application.Commands;
using ERP.Transport.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Transport.API;

[ApiController]
[Route("api/transport/routes")]
[Authorize]
public class RoutesController : ControllerBase
{
    private readonly IMediator _mediator;

    public RoutesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> CreateRoute([FromBody] CreateRouteRequest request, CancellationToken ct)
    {
        var cmd = new CreateRouteCommand(
            request.Name, request.Description, request.VehicleId, request.DriverId,
            TimeOnly.Parse(request.DepartureTime), TimeOnly.Parse(request.ReturnTime));
        var result = await _mediator.Send(cmd, ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(new { id = result.Value });
    }

    [HttpGet]
    public async Task<IActionResult> GetRoutes([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetRoutesQuery(page, pageSize), ct);
        return Ok(result);
    }

    [HttpPost("{routeId:guid}/stops")]
    public async Task<IActionResult> AddStop(Guid routeId, [FromBody] AddStopRequest request, CancellationToken ct)
    {
        var pickupTime = request.PickupTime is not null ? TimeOnly.Parse(request.PickupTime) : (TimeOnly?)null;
        var cmd = new AddRouteStopCommand(routeId, request.Name, request.Sequence, pickupTime, request.DistanceFromCollegeKm);
        var result = await _mediator.Send(cmd, ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(new { id = result.Value });
    }

    [HttpGet("{routeId:guid}/stops")]
    public async Task<IActionResult> GetStops(Guid routeId, CancellationToken ct)
    {
        var stops = await _mediator.Send(new GetRouteStopsQuery(routeId), ct);
        return Ok(stops);
    }

    [HttpPost("{routeId:guid}/assign")]
    public async Task<IActionResult> Assign(Guid routeId, [FromBody] AssignRequest request, CancellationToken ct)
    {
        var cmd = new AssignToRouteCommand(routeId, request.StopId, request.MemberId,
            request.MemberType, request.MemberName, request.AcademicYear);
        var result = await _mediator.Send(cmd, ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(new { id = result.Value });
    }

    [HttpDelete("assignments/{assignmentId:guid}")]
    public async Task<IActionResult> RemoveAssignment(Guid assignmentId, CancellationToken ct)
    {
        var result = await _mediator.Send(new RemoveRouteAssignmentCommand(assignmentId), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return NoContent();
    }

    [HttpGet("{routeId:guid}/passengers")]
    public async Task<IActionResult> GetPassengers(Guid routeId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetRoutePassengersQuery(routeId, page, pageSize), ct);
        return Ok(result);
    }
}

public record CreateRouteRequest(
    string Name,
    string? Description,
    Guid? VehicleId,
    Guid? DriverId,
    string DepartureTime,
    string ReturnTime);

public record AddStopRequest(
    string Name,
    int Sequence,
    string? PickupTime,
    decimal? DistanceFromCollegeKm);

public record AssignRequest(
    Guid StopId,
    Guid MemberId,
    string MemberType,
    string MemberName,
    int AcademicYear);
