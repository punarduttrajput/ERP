using ERP.Placement.Application.Commands;
using ERP.Placement.Application.Queries;
using ERP.Placement.Domain;
using ERP.Shared.Application.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Placement.API;

[ApiController]
[Route("api/placement/drives")]
[Authorize]
public class DrivesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentTenant _currentTenant;

    public DrivesController(IMediator mediator, ICurrentTenant currentTenant)
    {
        _mediator = mediator;
        _currentTenant = currentTenant;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDriveRequest dto, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateDriveCommand(
            _currentTenant.TenantId!.Value,
            dto.CompanyId,
            dto.JobRole,
            dto.JobDescription,
            dto.Location,
            dto.PackageLpa,
            dto.MinCgpa,
            dto.MaxBacklogs,
            dto.EligibleBranches,
            dto.DriveDate,
            dto.RegistrationDeadline,
            dto.Status,
            dto.AcademicYear
        ), ct);

        if (!result.IsSuccess) return BadRequest(new { result.Error });
        return CreatedAtAction(nameof(GetDrive), new { id = result.Value }, new { id = result.Value });
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] DriveStatus? status = null,
        [FromQuery] int? academicYear = null,
        [FromQuery] Guid? companyId = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetDrivesQuery(page, pageSize, status, academicYear, companyId), ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetDrive(Guid id, [FromQuery] int page = 1, [FromQuery] int pageSize = 1, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetDrivesQuery(page, pageSize, null, null, null), ct);
        var drive = result.Items.FirstOrDefault(x => x.Id == id);
        if (drive is null) return NotFound();
        return Ok(drive);
    }

    [HttpPost("{id:guid}/register")]
    public async Task<IActionResult> Register(Guid id, [FromBody] RegisterForDriveRequest dto, CancellationToken ct)
    {
        var result = await _mediator.Send(new RegisterForDriveCommand(
            _currentTenant.TenantId!.Value,
            id,
            dto.StudentId,
            dto.StudentName,
            dto.StudentCgpa,
            dto.ActiveBacklogs,
            dto.Branch
        ), ct);

        if (!result.IsSuccess) return BadRequest(new { result.Error });
        return Ok(new { registrationId = result.Value });
    }

    [HttpGet("{id:guid}/registrations")]
    public async Task<IActionResult> GetRegistrations(Guid id, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetDriveRegistrationsQuery(id, page, pageSize), ct);
        return Ok(result);
    }

    [HttpPatch("registrations/{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateStatusRequest dto, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateRegistrationStatusCommand(
            _currentTenant.TenantId!.Value,
            id,
            dto.NewStatus,
            dto.Notes,
            dto.InterviewAt,
            dto.OfferLpa
        ), ct);

        if (!result.IsSuccess) return BadRequest(new { result.Error });
        return NoContent();
    }

    [HttpGet("students/{studentId:guid}")]
    public async Task<IActionResult> GetStudentPlacement(Guid studentId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetStudentPlacementQuery(studentId), ct);
        if (!result.IsSuccess) return BadRequest(new { result.Error });
        return Ok(result.Value);
    }

    [HttpPost("offers/{offerId:guid}/confirm")]
    public async Task<IActionResult> ConfirmOffer(Guid offerId, [FromBody] ConfirmOfferRequest dto, CancellationToken ct)
    {
        var result = await _mediator.Send(new ConfirmOfferCommand(offerId, dto.Accept), ct);
        if (!result.IsSuccess) return BadRequest(new { result.Error });
        return NoContent();
    }
}

public record CreateDriveRequest(
    Guid CompanyId,
    string JobRole,
    string? JobDescription,
    string? Location,
    decimal PackageLpa,
    decimal MinCgpa,
    int MaxBacklogs,
    string? EligibleBranches,
    DateOnly? DriveDate,
    DateOnly? RegistrationDeadline,
    DriveStatus Status,
    int AcademicYear
);

public record RegisterForDriveRequest(
    Guid StudentId,
    string StudentName,
    decimal StudentCgpa,
    int ActiveBacklogs,
    string Branch
);

public record UpdateStatusRequest(
    RegistrationStatus NewStatus,
    string? Notes,
    DateTime? InterviewAt,
    decimal? OfferLpa
);

public record ConfirmOfferRequest(bool Accept);
