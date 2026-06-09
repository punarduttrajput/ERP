using ERP.HRMS.Application.Commands;
using ERP.HRMS.Application.Queries;
using ERP.Shared.Application.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.HRMS.API;

[Authorize]
[ApiController]
[Route("api/hrms/appraisal")]
public class AppraisalController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentTenant _tenant;
    private readonly ICurrentUser _user;

    public AppraisalController(IMediator mediator, ICurrentTenant tenant, ICurrentUser user)
    {
        _mediator = mediator;
        _tenant = tenant;
        _user = user;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAppraisalRequest dto, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateAppraisalCommand(
            _tenant.TenantId!.Value, dto.EmployeeId, dto.ReviewYear
        ), ct);

        if (!result.IsSuccess) return BadRequest(new { result.Error });
        return Ok(new { id = result.Value });
    }

    [HttpPost("{id:guid}/self-assessment")]
    public async Task<IActionResult> SelfAssessment(Guid id, [FromBody] SelfAssessmentRequest dto, CancellationToken ct)
    {
        var result = await _mediator.Send(new SubmitSelfAssessmentCommand(
            id, _tenant.TenantId!.Value, dto.Assessment, dto.Rating
        ), ct);

        if (!result.IsSuccess) return BadRequest(new { result.Error });
        return NoContent();
    }

    [HttpPost("{id:guid}/manager-review")]
    public async Task<IActionResult> ManagerReview(Guid id, [FromBody] ManagerReviewRequest dto, CancellationToken ct)
    {
        var result = await _mediator.Send(new SubmitManagerReviewCommand(
            id, _tenant.TenantId!.Value, dto.Review, dto.Rating
        ), ct);

        if (!result.IsSuccess) return BadRequest(new { result.Error });
        return NoContent();
    }

    [HttpPost("{id:guid}/finalise")]
    public async Task<IActionResult> Finalise(Guid id, [FromBody] FinaliseAppraisalRequest dto, CancellationToken ct)
    {
        var result = await _mediator.Send(new FinaliseAppraisalCommand(
            id, _tenant.TenantId!.Value, dto.HrComments, dto.FinalRating, _user.UserId!.Value
        ), ct);

        if (!result.IsSuccess) return BadRequest(new { result.Error });
        return NoContent();
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetAppraisalQuery(id, _tenant.TenantId!.Value), ct);
        if (!result.IsSuccess) return NotFound(new { result.Error });
        return Ok(result.Value);
    }
}

public record CreateAppraisalRequest(Guid EmployeeId, int ReviewYear);
public record SelfAssessmentRequest(string Assessment, decimal Rating);
public record ManagerReviewRequest(string Review, decimal Rating);
public record FinaliseAppraisalRequest(string HrComments, decimal FinalRating);
