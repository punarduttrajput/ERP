using ERP.OBE.Application.Commands;
using ERP.OBE.Application.Queries;
using ERP.OBE.Domain;
using ERP.Shared.Application.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.OBE.API;

[ApiController]
[Route("api/obe")]
[Authorize]
public class ObeController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentTenant _currentTenant;

    public ObeController(IMediator mediator, ICurrentTenant currentTenant)
    {
        _mediator = mediator;
        _currentTenant = currentTenant;
    }

    [HttpPost("mappings/co-po")]
    public async Task<IActionResult> SetCoPoMapping([FromBody] SetCoPoMappingDto dto, CancellationToken ct)
    {
        var result = await _mediator.Send(
            new SetCoPoMappingCommand(_currentTenant.TenantId!.Value, dto.SubjectId, dto.ProgramId,
                dto.Mappings.Select(m => new CoPoMappingItem(m.CourseOutcomeCode, m.ProgramOutcomeCode, m.CorrelationLevel)).ToList()), ct);
        if (!result.IsSuccess) return BadRequest(result.Error);
        return NoContent();
    }

    [HttpGet("mappings/{programId:guid}")]
    public async Task<IActionResult> GetCoPoMatrix(Guid programId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetCoPoMatrixQuery(_currentTenant.TenantId!.Value, programId), ct);
        if (!result.IsSuccess) return BadRequest(result.Error);
        return Ok(result.Value);
    }

    [HttpPost("attainment/direct/compute")]
    public async Task<IActionResult> ComputeDirectAttainment([FromBody] ComputeDirectAttainmentDto dto, CancellationToken ct)
    {
        var result = await _mediator.Send(
            new ComputeDirectAttainmentCommand(_currentTenant.TenantId!.Value, dto.SubjectId, dto.SemesterId,
                dto.AcademicYear, dto.ThresholdPercent), ct);
        if (!result.IsSuccess) return BadRequest(result.Error);
        return Ok(new { coCount = result.Value });
    }

    [HttpGet("attainment/summary")]
    public async Task<IActionResult> GetAttainmentSummary([FromQuery] Guid subjectId, [FromQuery] Guid semesterId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetAttainmentSummaryQuery(_currentTenant.TenantId!.Value, subjectId, semesterId), ct);
        if (!result.IsSuccess) return BadRequest(result.Error);
        return Ok(result.Value);
    }

    [HttpGet("attainment/gaps")]
    public async Task<IActionResult> GetGapAnalysis([FromQuery] Guid subjectId, [FromQuery] Guid semesterId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetGapAnalysisQuery(_currentTenant.TenantId!.Value, subjectId, semesterId), ct);
        if (!result.IsSuccess) return BadRequest(result.Error);
        return Ok(result.Value);
    }

    [HttpPost("action-plans/generate")]
    public async Task<IActionResult> GenerateActionPlans([FromBody] GenerateActionPlanDto dto, CancellationToken ct)
    {
        var result = await _mediator.Send(
            new GenerateActionPlanCommand(_currentTenant.TenantId!.Value, dto.SubjectId, dto.SemesterId, dto.AcademicYear), ct);
        if (!result.IsSuccess) return BadRequest(result.Error);
        return Ok(new { created = result.Value });
    }

    [HttpPatch("action-plans/{id:guid}")]
    public async Task<IActionResult> UpdateActionPlan(Guid id, [FromBody] UpdateActionPlanDto dto, CancellationToken ct)
    {
        var result = await _mediator.Send(
            new UpdateActionPlanCommand(_currentTenant.TenantId!.Value, id, dto.Status, dto.Outcome, dto.AssignedTo, dto.TargetDate), ct);
        if (!result.IsSuccess) return BadRequest(result.Error);
        return NoContent();
    }
}

public record SetCoPoMappingDto(Guid SubjectId, Guid ProgramId, IReadOnlyList<CoPoMappingItemDto> Mappings);
public record CoPoMappingItemDto(string CourseOutcomeCode, string ProgramOutcomeCode, int CorrelationLevel);
public record ComputeDirectAttainmentDto(Guid SubjectId, Guid SemesterId, int AcademicYear, decimal ThresholdPercent = 60m);
public record GenerateActionPlanDto(Guid SubjectId, Guid SemesterId, int AcademicYear);
public record UpdateActionPlanDto(ActionPlanStatus Status, string? Outcome, Guid? AssignedTo, DateOnly? TargetDate);
