using ERP.Placement.Application.Commands;
using ERP.Placement.Application.Queries;
using ERP.Shared.Application.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Placement.API;

[ApiController]
[Route("api/placement/companies")]
[Authorize]
public class CompaniesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentTenant _currentTenant;

    public CompaniesController(IMediator mediator, ICurrentTenant currentTenant)
    {
        _mediator = mediator;
        _currentTenant = currentTenant;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCompanyRequest dto, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateCompanyCommand(
            _currentTenant.TenantId!.Value,
            dto.Name,
            dto.Industry,
            dto.Website,
            dto.Description,
            dto.LogoUrl,
            dto.ContactPersonName,
            dto.ContactEmail,
            dto.ContactMobile
        ), ct);

        if (!result.IsSuccess) return BadRequest(new { result.Error });
        return CreatedAtAction(nameof(GetDrives), new { id = result.Value }, new { id = result.Value });
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? industry = null,
        [FromQuery] bool? isActive = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetCompaniesQuery(page, pageSize, industry, isActive), ct);
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCompanyRequest dto, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateCompanyCommand(
            id,
            dto.Name,
            dto.Industry,
            dto.Website,
            dto.Description,
            dto.LogoUrl,
            dto.ContactPersonName,
            dto.ContactEmail,
            dto.ContactMobile,
            dto.IsActive
        ), ct);

        if (!result.IsSuccess) return BadRequest(new { result.Error });
        return NoContent();
    }

    [HttpGet("{id:guid}/drives")]
    public async Task<IActionResult> GetDrives(Guid id, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetDrivesQuery(page, pageSize, null, null, id), ct);
        return Ok(result);
    }
}

public record CreateCompanyRequest(
    string Name,
    string Industry,
    string? Website,
    string? Description,
    string? LogoUrl,
    string? ContactPersonName,
    string? ContactEmail,
    string? ContactMobile
);

public record UpdateCompanyRequest(
    string Name,
    string Industry,
    string? Website,
    string? Description,
    string? LogoUrl,
    string? ContactPersonName,
    string? ContactEmail,
    string? ContactMobile,
    bool IsActive
);
