using ERP.Tenants.API.Dtos;
using ERP.Tenants.Application.Commands;
using ERP.Tenants.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Tenants.API;

[ApiController]
[Route("api/tenants")]
[Authorize(Roles = "SuperAdmin")]
public class TenantsController : ControllerBase
{
    private readonly IMediator _mediator;

    public TenantsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null, [FromQuery] string? status = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetAllTenantsQuery(page, pageSize, search, status), cancellationToken);
        if (!result.IsSuccess)
            return BadRequest(ApiResponse.Fail(result.Error!, HttpContext.TraceIdentifier));

        return Ok(ApiResponse.Ok(result.Value, HttpContext.TraceIdentifier));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetTenantQuery(id), cancellationToken);
        if (!result.IsSuccess)
            return NotFound(ApiResponse.Fail(result.Error!, HttpContext.TraceIdentifier));

        return Ok(ApiResponse.Ok(result.Value, HttpContext.TraceIdentifier));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTenantDto dto, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new CreateTenantCommand(dto.Name, dto.Slug, dto.ContactEmail, dto.ContactPhone, dto.Address, dto.Plan),
            cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(ApiResponse.Fail(result.Error!, HttpContext.TraceIdentifier));

        return CreatedAtAction(nameof(GetById), new { id = result.Value },
            ApiResponse.Ok(new { id = result.Value }, HttpContext.TraceIdentifier));
    }

    [HttpPut("{id:guid}/branding")]
    public async Task<IActionResult> UpdateBranding(Guid id, [FromBody] UpdateBrandingDto dto, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new UpdateTenantBrandingCommand(id, dto.LogoUrl, dto.PrimaryColor, dto.SecondaryColor, dto.CustomDomain, dto.CustomCss),
            cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(ApiResponse.Fail(result.Error!, HttpContext.TraceIdentifier));

        return Ok(ApiResponse.Ok(new { }, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:guid}/suspend")]
    public async Task<IActionResult> Suspend(Guid id, [FromBody] SuspendTenantDto dto, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new SuspendTenantCommand(id, dto.Reason), cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(ApiResponse.Fail(result.Error!, HttpContext.TraceIdentifier));

        return Ok(ApiResponse.Ok(new { }, HttpContext.TraceIdentifier));
    }
}

public record UpdateBrandingDto(
    string? LogoUrl,
    string? PrimaryColor,
    string? SecondaryColor,
    string? CustomDomain,
    string? CustomCss
);

public record SuspendTenantDto(string Reason);
