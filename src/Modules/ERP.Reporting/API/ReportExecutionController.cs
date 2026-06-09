using ERP.Reporting.Application.Commands;
using ERP.Reporting.Application.Queries;
using ERP.Reporting.Domain;
using ERP.Shared.Application.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Reporting.API;

[ApiController]
[Route("api/reports/execute")]
[Authorize]
public class ReportExecutionController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentTenant _currentTenant;
    private readonly ICurrentUser _currentUser;

    public ReportExecutionController(IMediator mediator, ICurrentTenant currentTenant, ICurrentUser currentUser)
    {
        _mediator = mediator;
        _currentTenant = currentTenant;
        _currentUser = currentUser;
    }

    [HttpPost("run")]
    public async Task<IActionResult> Run([FromBody] RunReportRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ExecuteReportCommand(
            _currentTenant.TenantId ?? Guid.Empty,
            request.ReportDefinitionId,
            request.ReportCode,
            request.FiltersJson,
            _currentUser.UserId), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export(
        [FromQuery] Guid? reportDefinitionId,
        [FromQuery] string? reportCode,
        [FromQuery] string? filtersJson,
        [FromQuery] ExportFormat format = ExportFormat.Excel,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new ExportReportCommand(
            _currentTenant.TenantId ?? Guid.Empty,
            reportDefinitionId,
            reportCode,
            filtersJson,
            format,
            null,
            _currentUser.UserId), cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(result.Error);

        return File(result.Value!.Content, result.Value.ContentType, result.Value.FileName);
    }

    [HttpGet("history")]
    public async Task<IActionResult> History(
        [FromQuery] Guid? reportId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetReportExecutionsQuery(
            _currentTenant.TenantId ?? Guid.Empty, reportId, page, pageSize), cancellationToken);
        return Ok(result);
    }
}

public record RunReportRequest(Guid? ReportDefinitionId, string? ReportCode, string? FiltersJson);
