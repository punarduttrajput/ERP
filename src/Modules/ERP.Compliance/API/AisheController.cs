using ERP.Compliance.Application.Commands;
using ERP.Compliance.Application.Queries;
using ERP.Shared.Application.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Compliance.API;

[ApiController]
[Route("api/compliance/aishe")]
[Authorize]
public class AisheController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentTenant _currentTenant;

    public AisheController(IMediator mediator, ICurrentTenant currentTenant)
    {
        _mediator = mediator;
        _currentTenant = currentTenant;
    }

    [HttpPost("compile")]
    public async Task<IActionResult> Compile([FromQuery] int academicYear, CancellationToken ct = default)
    {
        var tenantId = _currentTenant.TenantId ?? Guid.Empty;
        var result = await _mediator.Send(new CompileAisheReturnCommand(tenantId, academicYear), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok(new { id = result.Value });
    }

    [HttpGet("{academicYear:int}")]
    public async Task<IActionResult> Get(int academicYear, CancellationToken ct = default)
    {
        var tenantId = _currentTenant.TenantId ?? Guid.Empty;
        var result = await _mediator.Send(new GetAisheReturnQuery(tenantId, academicYear), ct);
        if (!result.IsSuccess) return NotFound(new { error = result.Error });
        return Ok(result.Value);
    }

    [HttpPut("{academicYear:int}")]
    public async Task<IActionResult> Update(int academicYear, [FromBody] UpdateAisheReturnRequest body, CancellationToken ct = default)
    {
        var tenantId = _currentTenant.TenantId ?? Guid.Empty;
        var result = await _mediator.Send(new UpdateAisheReturnCommand(
            tenantId, academicYear, body.InstitutionType, body.EstablishmentYear,
            body.TotalProgrammes, body.TotalDepartments, body.TotalStudentsEnrolled,
            body.MaleStudents, body.FemaleStudents, body.ScStudents, body.StStudents, body.ObcStudents,
            body.TotalFaculty, body.MaleFaculty, body.FemaleFaculty, body.PhdFaculty,
            body.TotalBuiltAreaSqm, body.TotalLibraryBooks), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return NoContent();
    }

    [HttpPost("{academicYear:int}/submit")]
    public async Task<IActionResult> Submit(int academicYear, [FromBody] SubmitAisheRequest body, CancellationToken ct = default)
    {
        var tenantId = _currentTenant.TenantId ?? Guid.Empty;
        var result = await _mediator.Send(new SubmitAisheReturnCommand(tenantId, academicYear, body.SubmissionReference), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return NoContent();
    }
}

public record UpdateAisheReturnRequest(
    string? InstitutionType,
    int? EstablishmentYear,
    int? TotalProgrammes,
    int? TotalDepartments,
    int? TotalStudentsEnrolled,
    int? MaleStudents,
    int? FemaleStudents,
    int? ScStudents,
    int? StStudents,
    int? ObcStudents,
    int? TotalFaculty,
    int? MaleFaculty,
    int? FemaleFaculty,
    int? PhdFaculty,
    decimal? TotalBuiltAreaSqm,
    int? TotalLibraryBooks);

public record SubmitAisheRequest(string SubmissionReference);
