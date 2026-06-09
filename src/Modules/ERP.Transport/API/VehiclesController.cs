using ERP.Transport.Application.Commands;
using ERP.Transport.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Transport.API;

[ApiController]
[Route("api/transport")]
[Authorize]
public class VehiclesController : ControllerBase
{
    private readonly IMediator _mediator;

    public VehiclesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("vehicles")]
    public async Task<IActionResult> CreateVehicle([FromBody] CreateVehicleRequest request, CancellationToken ct)
    {
        var cmd = new CreateVehicleCommand(
            request.RegistrationNumber, request.Make, request.Model, request.Capacity,
            request.FitnessExpiryDate, request.InsuranceExpiryDate, request.PollutionExpiryDate);
        var result = await _mediator.Send(cmd, ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(new { id = result.Value });
    }

    [HttpGet("vehicles")]
    public async Task<IActionResult> GetVehicles([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetVehiclesQuery(page, pageSize), ct);
        return Ok(result);
    }

    [HttpPost("drivers")]
    public async Task<IActionResult> CreateDriver([FromBody] CreateDriverRequest request, CancellationToken ct)
    {
        var cmd = new CreateDriverCommand(request.Name, request.LicenseNumber, request.LicenseExpiryDate, request.MobileNumber);
        var result = await _mediator.Send(cmd, ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(new { id = result.Value });
    }

    [HttpGet("drivers")]
    public async Task<IActionResult> GetDrivers([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetDriversQuery(page, pageSize), ct);
        return Ok(result);
    }

    [HttpGet("locations/live")]
    public async Task<IActionResult> GetLiveLocations(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetLiveLocationsQuery(), ct);
        return Ok(result);
    }
}

public record CreateVehicleRequest(
    string RegistrationNumber,
    string Make,
    string Model,
    int Capacity,
    DateOnly FitnessExpiryDate,
    DateOnly InsuranceExpiryDate,
    DateOnly PollutionExpiryDate);

public record CreateDriverRequest(
    string Name,
    string LicenseNumber,
    DateOnly LicenseExpiryDate,
    string MobileNumber);
