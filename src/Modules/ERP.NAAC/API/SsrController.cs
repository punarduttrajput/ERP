using ERP.NAAC.Application.Commands;
using ERP.NAAC.Application.Queries;
using ERP.NAAC.Domain;
using ERP.Shared.Application.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.NAAC.API;

[ApiController]
[Route("api/naac/ssr")]
[Authorize]
public class SsrController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentTenant _currentTenant;
    private readonly ICurrentUser _currentUser;

    public SsrController(IMediator mediator, ICurrentTenant currentTenant, ICurrentUser currentUser)
    {
        _mediator = mediator;
        _currentTenant = currentTenant;
        _currentUser = currentUser;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSsrDto dto, CancellationToken ct)
    {
        var result = await _mediator.Send(
            new CreateSsrCommand(_currentTenant.TenantId!.Value, dto.AcademicYear, dto.Title), ct);
        if (!result.IsSuccess) return BadRequest(result.Error);
        return CreatedAtAction(nameof(Get), new { id = result.Value }, new { id = result.Value });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetSsrQuery(id), ct);
        if (!result.IsSuccess) return NotFound(result.Error);
        return Ok(result.Value);
    }

    [HttpPut("{id:guid}/sections/{sectionId:guid}")]
    public async Task<IActionResult> UpdateSection(Guid id, Guid sectionId, [FromBody] UpdateSsrSectionDto dto, CancellationToken ct)
    {
        var result = await _mediator.Send(
            new UpdateSsrSectionCommand(id, sectionId, dto.Content, _currentUser.UserId!.Value), ct);
        if (!result.IsSuccess) return BadRequest(result.Error);
        return NoContent();
    }

    [HttpPost("{id:guid}/generate-pdf")]
    public async Task<IActionResult> GeneratePdf(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GenerateSsrPdfCommand(id), ct);
        if (!result.IsSuccess) return BadRequest(result.Error);
        return File(result.Value!, "application/pdf", $"SSR_{id}.pdf");
    }

    [HttpPost("{id:guid}/submit")]
    public async Task<IActionResult> Submit(Guid id, CancellationToken ct)
    {
        // Direct status update — no separate command needed; SSR submission is a lightweight state change
        var result = await _mediator.Send(new SubmitSsrCommand(id), ct);
        if (!result.IsSuccess) return BadRequest(result.Error);
        return NoContent();
    }
}

public record CreateSsrDto(int AcademicYear, string Title);
public record UpdateSsrSectionDto(string Content);
