using ERP.Accreditation.Application.Commands;
using ERP.Accreditation.Application.Queries;
using ERP.Shared.Application.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Accreditation.API;

[ApiController]
[Route("api/accreditation/evidence")]
[Authorize]
public class EvidenceController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentTenant _currentTenant;
    private readonly ICurrentUser _currentUser;

    public EvidenceController(IMediator mediator, ICurrentTenant currentTenant, ICurrentUser currentUser)
    {
        _mediator = mediator;
        _currentTenant = currentTenant;
        _currentUser = currentUser;
    }

    [HttpPost("tags")]
    public async Task<IActionResult> TagRecord([FromBody] TagEvidenceRequest dto, CancellationToken ct)
    {
        var result = await _mediator.Send(new TagEvidenceCommand(
            _currentTenant.TenantId!.Value,
            dto.ModuleName,
            dto.RecordId,
            dto.RecordLabel,
            dto.NaacCriterion,
            dto.NaacIndicator,
            dto.Notes,
            _currentUser.UserId!.Value
        ), ct);

        if (!result.IsSuccess) return BadRequest(new { result.Error });
        return CreatedAtAction(nameof(GetTags), new { }, new { id = result.Value });
    }

    [HttpGet("tags")]
    public async Task<IActionResult> GetTags(
        [FromQuery] string? criterion = null,
        [FromQuery] string? module = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetEvidenceTagsQuery(
            _currentTenant.TenantId!.Value,
            criterion,
            module,
            page,
            pageSize
        ), ct);

        return Ok(result);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshEvidenceRequest dto, CancellationToken ct)
    {
        var result = await _mediator.Send(new RefreshEvidenceSummaryCommand(
            _currentTenant.TenantId!.Value,
            dto.AcademicYear,
            dto.ModuleFilter
        ), ct);

        if (!result.IsSuccess) return BadRequest(new { result.Error });
        return Ok(new { summariesRefreshed = result.Value });
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(
        [FromQuery] int academicYear,
        [FromQuery] string? module = null,
        [FromQuery] string? category = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetEvidenceSummaryQuery(
            _currentTenant.TenantId!.Value,
            academicYear,
            module,
            category
        ), ct);

        return Ok(result);
    }

    [HttpGet("coverage")]
    public async Task<IActionResult> GetCoverage(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetEvidenceCoverageQuery(_currentTenant.TenantId!.Value), ct);
        return Ok(result);
    }
}

public record TagEvidenceRequest(
    string ModuleName,
    string RecordId,
    string RecordLabel,
    string NaacCriterion,
    string NaacIndicator,
    string? Notes
);

public record RefreshEvidenceRequest(
    int AcademicYear,
    string[]? ModuleFilter
);
