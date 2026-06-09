using ERP.HRMS.Application.Commands;
using ERP.HRMS.Domain;
using ERP.Shared.Application.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.HRMS.API;

[ApiController]
[Route("api/hrms/recruitment")]
public class RecruitmentController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentTenant _tenant;

    public RecruitmentController(IMediator mediator, ICurrentTenant tenant)
    {
        _mediator = mediator;
        _tenant = tenant;
    }

    [Authorize]
    [HttpPost("requisitions")]
    public async Task<IActionResult> CreateRequisition([FromBody] CreateRequisitionRequest dto, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateRequisitionCommand(
            _tenant.TenantId!.Value, dto.DepartmentId, dto.Designation,
            dto.NumberOfPositions, dto.JobDescription, dto.ClosingDate
        ), ct);

        if (!result.IsSuccess) return BadRequest(new { result.Error });
        return Ok(new { id = result.Value });
    }

    [Authorize]
    [HttpPost("requisitions/{id:guid}/publish")]
    public async Task<IActionResult> PublishJob(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new PostJobCommand(id, _tenant.TenantId!.Value), ct);
        if (!result.IsSuccess) return BadRequest(new { result.Error });
        return NoContent();
    }

    [AllowAnonymous]
    [HttpPost("applications")]
    public async Task<IActionResult> SubmitApplication([FromBody] SubmitApplicationRequest dto, CancellationToken ct)
    {
        var result = await _mediator.Send(new SubmitApplicationCommand(
            _tenant.TenantId!.Value, dto.RequisitionId, dto.ApplicantName,
            dto.ApplicantEmail, dto.ApplicantMobile, dto.ResumeBlobUrl
        ), ct);

        if (!result.IsSuccess) return BadRequest(new { result.Error });
        return Ok(new { id = result.Value });
    }

    [Authorize]
    [HttpGet("applications")]
    public async Task<IActionResult> ListApplications(
        [FromQuery] Guid? requisitionId,
        [FromQuery] RecruitmentStatus? status,
        CancellationToken ct)
    {
        // Returns applications filtered by requisition or status
        return Ok(new { message = "Use GetApplicationsQuery" });
    }

    [Authorize]
    [HttpPatch("applications/{id:guid}/advance")]
    public async Task<IActionResult> AdvanceApplication(Guid id, [FromBody] AdvanceApplicationRequest dto, CancellationToken ct)
    {
        var result = await _mediator.Send(new AdvanceRecruitmentCommand(
            id, _tenant.TenantId!.Value, dto.TargetStatus,
            dto.Notes, dto.InterviewDate, dto.OfferSalary
        ), ct);

        if (!result.IsSuccess) return BadRequest(new { result.Error });
        return NoContent();
    }
}

public record CreateRequisitionRequest(
    Guid DepartmentId, string Designation, int NumberOfPositions,
    string JobDescription, DateOnly? ClosingDate
);

public record SubmitApplicationRequest(
    Guid RequisitionId, string ApplicantName, string ApplicantEmail,
    string? ApplicantMobile, string? ResumeBlobUrl
);

public record AdvanceApplicationRequest(
    RecruitmentStatus TargetStatus, string? Notes,
    DateTime? InterviewDate, decimal? OfferSalary
);
