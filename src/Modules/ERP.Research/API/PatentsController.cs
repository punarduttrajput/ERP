using ERP.Research.Application.Commands;
using ERP.Research.Application.Queries;
using ERP.Research.Domain;
using ERP.Shared.Application.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Research.API;

[ApiController]
[Route("api/research/patents")]
[Authorize]
public class PatentsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentTenant _currentTenant;

    public PatentsController(IMediator mediator, ICurrentTenant currentTenant)
    {
        _mediator = mediator;
        _currentTenant = currentTenant;
    }

    private Guid TenantId => _currentTenant.TenantId ?? Guid.Empty;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePatentRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreatePatentCommand(
            TenantId, request.Title, request.Inventors, request.ApplicationNumber,
            request.FilingDate, request.PatentOffice, request.ResearchProjectId), ct);

        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok(new { patentId = result.Value });
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] PatentStatus? status,
        [FromQuery] Guid? projectId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetPatentsQuery(TenantId, status, projectId, page, pageSize), ct);
        return Ok(result);
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdatePatentStatusRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdatePatentStatusCommand(
            TenantId, id, request.NewStatus, request.GrantNumber, request.GrantDate), ct);

        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok();
    }
}

public record CreatePatentRequest(
    string Title,
    string Inventors,
    string? ApplicationNumber,
    DateOnly? FilingDate,
    string PatentOffice,
    Guid? ResearchProjectId);

public record UpdatePatentStatusRequest(PatentStatus NewStatus, string? GrantNumber, DateOnly? GrantDate);
