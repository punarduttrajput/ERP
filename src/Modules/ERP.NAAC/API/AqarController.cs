using ERP.NAAC.Application.Commands;
using ERP.NAAC.Application.Queries;
using ERP.Shared.Application.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.NAAC.API;

[ApiController]
[Route("api/naac/aqar")]
[Authorize]
public class AqarController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentTenant _currentTenant;
    private readonly ICurrentUser _currentUser;

    public AqarController(IMediator mediator, ICurrentTenant currentTenant, ICurrentUser currentUser)
    {
        _mediator = mediator;
        _currentTenant = currentTenant;
        _currentUser = currentUser;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAqarDto dto, CancellationToken ct)
    {
        var result = await _mediator.Send(
            new CreateAqarCommand(_currentTenant.TenantId!.Value, dto.AcademicYear, dto.Title), ct);
        if (!result.IsSuccess) return BadRequest(result.Error);
        return CreatedAtAction(nameof(Get), new { id = result.Value }, new { id = result.Value });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetAqarQuery(id), ct);
        if (!result.IsSuccess) return NotFound(result.Error);
        return Ok(result.Value);
    }

    [HttpPost("{id:guid}/sections/{sectionId:guid}/assign")]
    public async Task<IActionResult> Assign(Guid id, Guid sectionId, [FromBody] AssignSectionDto dto, CancellationToken ct)
    {
        var result = await _mediator.Send(new AssignAqarSectionCommand(id, sectionId, dto.AssignedTo), ct);
        if (!result.IsSuccess) return BadRequest(result.Error);
        return NoContent();
    }

    [HttpPost("{id:guid}/sections/{sectionId:guid}/submit")]
    public async Task<IActionResult> SubmitSection(Guid id, Guid sectionId, [FromBody] SubmitSectionDto dto, CancellationToken ct)
    {
        var result = await _mediator.Send(new SubmitAqarSectionCommand(id, sectionId, dto.Content), ct);
        if (!result.IsSuccess) return BadRequest(result.Error);
        return NoContent();
    }

    [HttpPost("{id:guid}/sections/{sectionId:guid}/review")]
    public async Task<IActionResult> ReviewSection(Guid id, Guid sectionId, [FromBody] ReviewSectionDto dto, CancellationToken ct)
    {
        var result = await _mediator.Send(
            new ReviewAqarSectionCommand(id, sectionId, dto.Approved, _currentUser.UserId!.Value, dto.Comment), ct);
        if (!result.IsSuccess) return BadRequest(result.Error);
        return NoContent();
    }

    [HttpPost("{id:guid}/finalise")]
    public async Task<IActionResult> Finalise(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new FinaliseAqarCommand(id), ct);
        if (!result.IsSuccess) return BadRequest(result.Error);
        return NoContent();
    }
}

public record CreateAqarDto(int AcademicYear, string Title);
public record AssignSectionDto(Guid AssignedTo);
public record SubmitSectionDto(string Content);
public record ReviewSectionDto(bool Approved, string? Comment);
