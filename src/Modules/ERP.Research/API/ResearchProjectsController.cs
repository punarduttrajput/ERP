using ERP.Research.Application.Commands;
using ERP.Research.Application.Queries;
using ERP.Research.Domain;
using ERP.Shared.Application.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Research.API;

[ApiController]
[Route("api/research/projects")]
[Authorize]
public class ResearchProjectsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentTenant _currentTenant;

    public ResearchProjectsController(IMediator mediator, ICurrentTenant currentTenant)
    {
        _mediator = mediator;
        _currentTenant = currentTenant;
    }

    private Guid TenantId => _currentTenant.TenantId ?? Guid.Empty;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateResearchProjectRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateResearchProjectCommand(
            TenantId, request.Title, request.PrincipalInvestigatorId, request.PrincipalInvestigatorName,
            request.FundingAgency, request.FundingScheme, request.SanctionedAmount,
            request.StartDate, request.EndDate, request.SanctionNumber, request.Abstract, request.Domain), ct);

        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok(new { projectId = result.Value });
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] ProjectStatus? status,
        [FromQuery] Guid? piId,
        [FromQuery] string? domain,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetResearchProjectsQuery(TenantId, status, piId, domain, page, pageSize), ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetResearchProjectsQuery(TenantId, null, null, null, 1, 1), ct);
        var project = result.Items.FirstOrDefault(x => x.Id == id);
        if (project is null) return NotFound(new { error = "Research project not found." });
        return Ok(project);
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateProjectStatusRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateProjectStatusCommand(TenantId, id, request.NewStatus, request.Notes), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok();
    }

    [HttpPost("{id:guid}/members")]
    public async Task<IActionResult> AddMember(Guid id, [FromBody] AddMemberRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new AddProjectMemberCommand(
            TenantId, id, request.UserId, request.MemberName, request.Role, request.JoinedAt), ct);

        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok(new { memberId = result.Value });
    }
}

public record CreateResearchProjectRequest(
    string Title,
    Guid PrincipalInvestigatorId,
    string PrincipalInvestigatorName,
    string FundingAgency,
    string? FundingScheme,
    decimal SanctionedAmount,
    DateOnly StartDate,
    DateOnly? EndDate,
    string? SanctionNumber,
    string? Abstract,
    string? Domain);

public record UpdateProjectStatusRequest(ProjectStatus NewStatus, string? Notes);

public record AddMemberRequest(Guid UserId, string MemberName, MemberRole Role, DateOnly JoinedAt);
