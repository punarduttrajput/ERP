using ERP.OBE.Application.Queries;
using ERP.Shared.Application.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.OBE.API;

[ApiController]
[Route("api/obe/nba-report")]
[Authorize]
public class NbaReportController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentTenant _currentTenant;

    public NbaReportController(IMediator mediator, ICurrentTenant currentTenant)
    {
        _mediator = mediator;
        _currentTenant = currentTenant;
    }

    [HttpGet]
    public async Task<IActionResult> GetReport([FromQuery] Guid programId, [FromQuery] int academicYear, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetNbaReportQuery(_currentTenant.TenantId!.Value, programId, academicYear), ct);
        if (!result.IsSuccess) return BadRequest(result.Error);
        return File(result.Value!, "application/pdf", $"NBA_Report_{programId}_{academicYear}.pdf");
    }
}
