using ERP.Research.Application.Commands;
using ERP.Research.Application.Queries;
using ERP.Research.Domain;
using ERP.Shared.Application.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Research.API;

[ApiController]
[Route("api/research/grants")]
[Authorize]
public class GrantsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentTenant _currentTenant;

    public GrantsController(IMediator mediator, ICurrentTenant currentTenant)
    {
        _mediator = mediator;
        _currentTenant = currentTenant;
    }

    private Guid TenantId => _currentTenant.TenantId ?? Guid.Empty;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateGrantRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateGrantCommand(
            TenantId, request.Title, request.FundingAgency, request.GrantNumber,
            request.SanctionedAmount, request.StartDate, request.EndDate,
            request.PrincipalInvestigatorId, request.ResearchProjectId), ct);

        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok(new { grantId = result.Value });
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] GrantStatus? status,
        [FromQuery] Guid? piId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetGrantsQuery(TenantId, status, piId, false, page, pageSize), ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetGrantsQuery(TenantId, null, null, true, 1, 1000), ct);
        var grant = result.Items.FirstOrDefault(x => x.Id == id);
        if (grant is null) return NotFound(new { error = "Grant not found." });
        return Ok(grant);
    }

    [HttpPost("{id:guid}/disbursements")]
    public async Task<IActionResult> RecordDisbursement(Guid id, [FromBody] RecordDisbursementRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new RecordDisbursementCommand(
            TenantId, id, request.Amount, request.DisbursedAt, request.Reference, request.Notes), ct);

        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok(new { disbursementId = result.Value });
    }

    [HttpPost("{id:guid}/close")]
    public async Task<IActionResult> Close(Guid id, [FromBody] CloseGrantRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CloseGrantCommand(
            TenantId, id, request.UtilizedAmount, request.UtilizationCertificateReference), ct);

        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok();
    }
}

public record CreateGrantRequest(
    string Title,
    string FundingAgency,
    string? GrantNumber,
    decimal SanctionedAmount,
    DateOnly? StartDate,
    DateOnly? EndDate,
    Guid PrincipalInvestigatorId,
    Guid? ResearchProjectId);

public record RecordDisbursementRequest(decimal Amount, DateOnly DisbursedAt, string? Reference, string? Notes);

public record CloseGrantRequest(decimal UtilizedAmount, string UtilizationCertificateReference);
