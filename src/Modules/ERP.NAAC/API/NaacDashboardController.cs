using ERP.NAAC.Application.Queries;
using ERP.NAAC.Domain;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.NAAC.API;

[ApiController]
[Route("api/naac/dashboard")]
[Authorize]
public class NaacDashboardController : ControllerBase
{
    private readonly IMediator _mediator;

    public NaacDashboardController(IMediator mediator) => _mediator = mediator;

    [HttpGet("criteria")]
    public IActionResult GetAllCriteria()
    {
        var list = NaacCriteria.All.Select(c => new
        {
            c.Number,
            c.Title,
            c.Indicators
        });
        return Ok(list);
    }

    [HttpGet("criteria/{number}")]
    public async Task<IActionResult> GetCriterionDashboard(string number, [FromQuery] int academicYear, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetCriterionDashboardQuery(number, academicYear), ct);
        if (!result.IsSuccess) return NotFound(result.Error);
        return Ok(result.Value);
    }

    [HttpGet("metrics")]
    public async Task<IActionResult> GetMetrics([FromQuery] int academicYear, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetNaacMetricsQuery(academicYear), ct);
        if (!result.IsSuccess) return BadRequest(result.Error);
        return Ok(result.Value);
    }
}
