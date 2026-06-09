using ERP.Research.Application.Commands;
using ERP.Research.Application.Queries;
using ERP.Research.Domain;
using ERP.Shared.Application.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Research.API;

[ApiController]
[Route("api/research/publications")]
[Authorize]
public class PublicationsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentTenant _currentTenant;

    public PublicationsController(IMediator mediator, ICurrentTenant currentTenant)
    {
        _mediator = mediator;
        _currentTenant = currentTenant;
    }

    private Guid TenantId => _currentTenant.TenantId ?? Guid.Empty;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePublicationRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreatePublicationCommand(
            TenantId, request.FacultyId, request.FacultyName, request.Title, request.PublicationType,
            request.VenueName, request.Isbn, request.IssueVolume, request.PageNumbers,
            request.PublicationYear, request.Doi, request.ImpactFactor, request.Index,
            request.IsUgcListed, request.ResearchProjectId), ct);

        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok(new { publicationId = result.Value });
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] Guid? facultyId,
        [FromQuery] PublicationType? type,
        [FromQuery] int? year,
        [FromQuery] PublicationIndex? index,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetPublicationsQuery(TenantId, facultyId, type, year, index, page, pageSize), ct);
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePublicationRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdatePublicationCommand(
            TenantId, id, request.Title, request.PublicationType, request.VenueName,
            request.Isbn, request.IssueVolume, request.PageNumbers, request.PublicationYear,
            request.Doi, request.ImpactFactor, request.Index, request.IsUgcListed, request.ResearchProjectId), ct);

        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok();
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetResearchDashboardQuery(TenantId), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok(result.Value);
    }
}

public record CreatePublicationRequest(
    Guid FacultyId,
    string FacultyName,
    string Title,
    PublicationType PublicationType,
    string VenueName,
    string? Isbn,
    string? IssueVolume,
    string? PageNumbers,
    int PublicationYear,
    string? Doi,
    decimal? ImpactFactor,
    PublicationIndex Index,
    bool IsUgcListed,
    Guid? ResearchProjectId);

public record UpdatePublicationRequest(
    string Title,
    PublicationType PublicationType,
    string VenueName,
    string? Isbn,
    string? IssueVolume,
    string? PageNumbers,
    int PublicationYear,
    string? Doi,
    decimal? ImpactFactor,
    PublicationIndex Index,
    bool IsUgcListed,
    Guid? ResearchProjectId);
