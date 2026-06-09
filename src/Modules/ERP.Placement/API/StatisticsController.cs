using ERP.Placement.Application.Queries;
using ERP.Shared.Application.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Placement.API;

[ApiController]
[Route("api/placement/statistics")]
[Authorize]
public class StatisticsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentTenant _currentTenant;

    public StatisticsController(IMediator mediator, ICurrentTenant currentTenant)
    {
        _mediator = mediator;
        _currentTenant = currentTenant;
    }

    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] int? academicYear = null,
        [FromQuery] int? totalEligibleStudents = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetPlacementStatisticsQuery(
            _currentTenant.TenantId!.Value,
            academicYear,
            totalEligibleStudents
        ), ct);

        if (!result.IsSuccess) return BadRequest(new { result.Error });
        return Ok(result.Value);
    }
}
