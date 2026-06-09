using ERP.Analytics.Application.Commands;
using ERP.Analytics.Application.Queries;
using ERP.Analytics.Domain;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ERP.Shared.Application.Abstractions;

namespace ERP.Analytics.API;

[ApiController]
[Route("api/analytics")]
[Authorize]
public class AnalyticsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentTenant _currentTenant;

    public AnalyticsController(IMediator mediator, ICurrentTenant currentTenant)
    {
        _mediator = mediator;
        _currentTenant = currentTenant;
    }

    [HttpPost("at-risk/compute")]
    public async Task<IActionResult> ComputeAtRisk([FromBody] ComputeAtRiskRequest request, CancellationToken ct)
    {
        var tenantId = _currentTenant.TenantId ?? Guid.Empty;
        var result = await _mediator.Send(new ComputeAtRiskScoresCommand(tenantId, request.SemesterId, request.AcademicYear), ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpGet("at-risk")]
    public async Task<IActionResult> GetAtRisk(
        [FromQuery] RiskLevel? riskLevel,
        [FromQuery] string? programName,
        [FromQuery] int? academicYear,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetAtRiskStudentsQuery(riskLevel, programName, academicYear, page, pageSize), ct);
        return Ok(result);
    }

    [HttpPost("fee-default/compute")]
    public async Task<IActionResult> ComputeFeeDefault([FromBody] ComputeFeeDefaultRequest request, CancellationToken ct)
    {
        var tenantId = _currentTenant.TenantId ?? Guid.Empty;
        var result = await _mediator.Send(new ComputeFeeDefaultRiskCommand(tenantId, request.AcademicYear), ct);
        return result.IsSuccess ? Ok(new { Processed = result.Value }) : BadRequest(result.Error);
    }

    [HttpGet("fee-default")]
    public async Task<IActionResult> GetFeeDefault(
        [FromQuery] RiskLevel? riskLevel,
        [FromQuery] int? academicYear,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetFeeDefaultRiskQuery(riskLevel, academicYear, page, pageSize), ct);
        return Ok(result);
    }

    [HttpPost("placement/compute")]
    public async Task<IActionResult> ComputePlacement([FromBody] ComputePlacementRequest request, CancellationToken ct)
    {
        var tenantId = _currentTenant.TenantId ?? Guid.Empty;
        var result = await _mediator.Send(new ComputePlacementScoresCommand(tenantId, request.AcademicYear), ct);
        return result.IsSuccess ? Ok(new { Processed = result.Value }) : BadRequest(result.Error);
    }

    [HttpGet("placement")]
    public async Task<IActionResult> GetPlacement(
        [FromQuery] int? academicYear,
        [FromQuery] string? programName,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetPlacementScoresQuery(academicYear, programName, page, pageSize), ct);
        return Ok(result);
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetAnalyticsDashboardQuery(), ct);
        return Ok(result);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(CancellationToken ct)
    {
        var tenantId = _currentTenant.TenantId ?? Guid.Empty;
        var year = DateTime.UtcNow.Year;

        var semesterId = Guid.Empty;
        var atRisk = await _mediator.Send(new ComputeAtRiskScoresCommand(tenantId, semesterId, year), ct);
        var feeDefault = await _mediator.Send(new ComputeFeeDefaultRiskCommand(tenantId, year), ct);
        var placement = await _mediator.Send(new ComputePlacementScoresCommand(tenantId, year), ct);

        return Ok(new
        {
            AtRiskScored = atRisk.Value?.TotalScored,
            FeeDefaultScored = feeDefault.Value,
            PlacementScored = placement.Value
        });
    }

    public record ComputeAtRiskRequest(Guid SemesterId, int AcademicYear);
    public record ComputeFeeDefaultRequest(int AcademicYear);
    public record ComputePlacementRequest(int AcademicYear);
}
