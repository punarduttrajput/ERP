using ERP.Alumni.Application.Commands;
using ERP.Alumni.Application.Queries;
using ERP.Shared.Application.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Alumni.API;

[ApiController]
[Route("api/alumni")]
[Authorize]
public class AlumniDirectoryController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentTenant _currentTenant;

    public AlumniDirectoryController(IMediator mediator, ICurrentTenant currentTenant)
    {
        _mediator = mediator;
        _currentTenant = currentTenant;
    }

    private Guid TenantId => _currentTenant.TenantId ?? Guid.Empty;

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterAlumniRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new RegisterAlumniCommand(
            TenantId, request.FirstName, request.LastName, request.Email,
            request.GraduationYear, request.ProgramName, request.BatchName, request.StudentId), ct);

        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok(new { alumniId = result.Value });
    }

    [HttpGet("directory")]
    public async Task<IActionResult> Directory(
        [FromQuery] int? graduationYear,
        [FromQuery] string? programName,
        [FromQuery] string? currentCity,
        [FromQuery] string? currentCountry,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new SearchAlumniDirectoryQuery(
            TenantId, graduationYear, programName, currentCity, currentCountry, search, page, pageSize), ct);

        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok(result.Value);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetAlumniProfileQuery(TenantId, id), ct);
        if (!result.IsSuccess) return NotFound(new { error = result.Error });
        return Ok(result.Value);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAlumniProfileRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateAlumniProfileCommand(
            TenantId, id, request.MobileNumber, request.CurrentEmployer, request.CurrentJobTitle,
            request.CurrentCity, request.CurrentCountry, request.LinkedInUrl, request.AvatarUrl), ct);

        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return NoContent();
    }

    [HttpPatch("{id:guid}/verify")]
    public async Task<IActionResult> Verify(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new VerifyAlumniCommand(TenantId, id), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return NoContent();
    }

    [HttpPatch("{id:guid}/visibility")]
    public async Task<IActionResult> ToggleVisibility(Guid id, [FromBody] ToggleVisibilityRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new ToggleAlumniVisibilityCommand(TenantId, id, request.IsDirectoryVisible), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return NoContent();
    }
}

public record RegisterAlumniRequest(
    string FirstName,
    string LastName,
    string Email,
    int GraduationYear,
    string ProgramName,
    string? BatchName,
    Guid? StudentId
);

public record UpdateAlumniProfileRequest(
    string? MobileNumber,
    string? CurrentEmployer,
    string? CurrentJobTitle,
    string? CurrentCity,
    string? CurrentCountry,
    string? LinkedInUrl,
    string? AvatarUrl
);

public record ToggleVisibilityRequest(bool IsDirectoryVisible);
