using ERP.Finance.Application.Queries;
using ERP.Shared.Application.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Finance.API;

[ApiController]
[Route("api/finance/reports")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentTenant _currentTenant;

    public ReportsController(IMediator mediator, ICurrentTenant currentTenant)
    {
        _mediator = mediator;
        _currentTenant = currentTenant;
    }

    [HttpGet("trial-balance")]
    public async Task<IActionResult> GetTrialBalance([FromQuery] DateOnly fromDate, [FromQuery] DateOnly toDate, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetTrialBalanceQuery(_currentTenant.TenantId!.Value, fromDate, toDate), ct);
        return Ok(result.Value);
    }

    [HttpGet("profit-loss")]
    public async Task<IActionResult> GetProfitAndLoss([FromQuery] DateOnly fromDate, [FromQuery] DateOnly toDate, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetProfitAndLossQuery(_currentTenant.TenantId!.Value, fromDate, toDate), ct);
        return Ok(result.Value);
    }

    [HttpGet("balance-sheet")]
    public async Task<IActionResult> GetBalanceSheet([FromQuery] DateOnly asOfDate, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetBalanceSheetQuery(_currentTenant.TenantId!.Value, asOfDate), ct);
        return Ok(result.Value);
    }
}
