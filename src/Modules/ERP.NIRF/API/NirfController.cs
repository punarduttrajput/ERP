using ERP.NIRF.Application.Commands;
using ERP.NIRF.Application.Queries;
using ERP.Shared.Application.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.NIRF.API;

[ApiController]
[Route("api/nirf")]
[Authorize]
public class NirfController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentTenant _currentTenant;

    public NirfController(IMediator mediator, ICurrentTenant currentTenant)
    {
        _mediator = mediator;
        _currentTenant = currentTenant;
    }

    [HttpPost("compile")]
    public async Task<IActionResult> Compile([FromQuery] int rankingYear, [FromQuery] string category = "University", CancellationToken ct = default)
    {
        var tenantId = _currentTenant.TenantId ?? Guid.Empty;
        var result = await _mediator.Send(new CompileNirfDataCommand(tenantId, rankingYear, category), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok(new { submissionId = result.Value });
    }

    [HttpGet("{submissionId:guid}")]
    public async Task<IActionResult> Get(Guid submissionId, CancellationToken ct = default)
    {
        var tenantId = _currentTenant.TenantId ?? Guid.Empty;
        var result = await _mediator.Send(new GetNirfSubmissionQuery(tenantId, submissionId), ct);
        if (!result.IsSuccess) return NotFound(new { error = result.Error });
        return Ok(result.Value);
    }

    [HttpPatch("{submissionId:guid}/parameters/{parameter}")]
    public async Task<IActionResult> UpdateParameter(Guid submissionId, string parameter, [FromBody] UpdateParameterRequest body, CancellationToken ct = default)
    {
        var tenantId = _currentTenant.TenantId ?? Guid.Empty;
        var result = await _mediator.Send(new UpdateNirfParameterCommand(tenantId, submissionId, parameter, body.RawScore, body.DataOverride), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return NoContent();
    }

    [HttpPost("{submissionId:guid}/finalise")]
    public async Task<IActionResult> Finalise(Guid submissionId, CancellationToken ct = default)
    {
        var tenantId = _currentTenant.TenantId ?? Guid.Empty;
        var result = await _mediator.Send(new FinaliseNirfSubmissionCommand(tenantId, submissionId), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return NoContent();
    }

    [HttpGet("analytics")]
    public async Task<IActionResult> Analytics([FromQuery] int years = 5, CancellationToken ct = default)
    {
        var tenantId = _currentTenant.TenantId ?? Guid.Empty;
        var result = await _mediator.Send(new GetNirfAnalyticsQuery(tenantId, years), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok(result.Value);
    }

    [HttpPost("rank-history")]
    public async Task<IActionResult> RecordRankHistory([FromBody] RecordRankHistoryRequest body, CancellationToken ct = default)
    {
        var tenantId = _currentTenant.TenantId ?? Guid.Empty;
        var result = await _mediator.Send(new RecordRankHistoryCommand(
            tenantId, body.RankingYear, body.Category, body.Rank, body.Score,
            body.TeachingLearningScore, body.ResearchScore, body.GraduationOutcomesScore,
            body.OutreachScore, body.PerceptionScore), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return NoContent();
    }

    [HttpGet("{submissionId:guid}/export/xml")]
    public async Task<IActionResult> ExportXml(Guid submissionId, CancellationToken ct = default)
    {
        var tenantId = _currentTenant.TenantId ?? Guid.Empty;
        var result = await _mediator.Send(new ExportNirfXmlQuery(tenantId, submissionId), ct);
        if (!result.IsSuccess) return NotFound(new { error = result.Error });
        return Content(result.Value!, "application/xml");
    }

    [HttpGet("{submissionId:guid}/export/pdf")]
    public async Task<IActionResult> ExportPdf(Guid submissionId, CancellationToken ct = default)
    {
        var tenantId = _currentTenant.TenantId ?? Guid.Empty;
        var result = await _mediator.Send(new ExportNirfPdfQuery(tenantId, submissionId), ct);
        if (!result.IsSuccess) return NotFound(new { error = result.Error });
        return File(result.Value!, "application/pdf", $"NIRF_{submissionId}.pdf");
    }
}

public record UpdateParameterRequest(decimal RawScore, object? DataOverride = null);

public record RecordRankHistoryRequest(
    int RankingYear,
    string Category,
    int? Rank,
    decimal? Score,
    decimal? TeachingLearningScore,
    decimal? ResearchScore,
    decimal? GraduationOutcomesScore,
    decimal? OutreachScore,
    decimal? PerceptionScore);
