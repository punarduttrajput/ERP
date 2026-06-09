using ERP.Reporting.Application.Commands;
using ERP.Reporting.Application.Queries;
using ERP.Reporting.Domain;
using ERP.Shared.Application.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Reporting.API;

[ApiController]
[Route("api/reports/definitions")]
[Authorize]
public class ReportDefinitionsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentTenant _currentTenant;
    private readonly ICurrentUser _currentUser;

    public ReportDefinitionsController(IMediator mediator, ICurrentTenant currentTenant, ICurrentUser currentUser)
    {
        _mediator = mediator;
        _currentTenant = currentTenant;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] ReportCategory? category,
        [FromQuery] bool? isBuiltIn,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetReportDefinitionsQuery(
            _currentTenant.TenantId ?? Guid.Empty, category, isBuiltIn, page, pageSize), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetReportDefinitionQuery(_currentTenant.TenantId ?? Guid.Empty, id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateReportDefinitionRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CreateReportDefinitionCommand(
            _currentTenant.TenantId ?? Guid.Empty,
            request.Name, request.Description, request.Category,
            request.SqlQuery, request.Columns, request.Filters, request.DefaultColumns), cancellationToken);
        return result.IsSuccess ? Ok(new { id = result.Value }) : BadRequest(result.Error);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateReportDefinitionRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new UpdateReportDefinitionCommand(
            id, _currentTenant.TenantId ?? Guid.Empty,
            request.Name, request.Description, request.Category,
            request.SqlQuery, request.DefaultColumns, request.IsActive), cancellationToken);
        return result.IsSuccess ? NoContent() : BadRequest(result.Error);
    }

    [HttpPost("seed")]
    public async Task<IActionResult> Seed(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new SeedPredefinedReportsCommand(_currentTenant.TenantId ?? Guid.Empty), cancellationToken);
        return result.IsSuccess ? Ok(new { seeded = result.Value }) : BadRequest(result.Error);
    }
}

public record CreateReportDefinitionRequest(
    string Name,
    string? Description,
    ReportCategory Category,
    string SqlQuery,
    ColumnDto[] Columns,
    FilterDto[] Filters,
    string[] DefaultColumns);

public record UpdateReportDefinitionRequest(
    string Name,
    string? Description,
    ReportCategory Category,
    string SqlQuery,
    string[] DefaultColumns,
    bool IsActive);
