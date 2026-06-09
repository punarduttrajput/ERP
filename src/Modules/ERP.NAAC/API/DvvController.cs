using ERP.NAAC.Application.Commands;
using ERP.NAAC.Application.Queries;
using ERP.NAAC.Domain;
using ERP.Shared.Application.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.NAAC.API;

[ApiController]
[Route("api/naac/dvv")]
[Authorize]
public class DvvController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentTenant _currentTenant;
    private readonly ICurrentUser _currentUser;

    public DvvController(IMediator mediator, ICurrentTenant currentTenant, ICurrentUser currentUser)
    {
        _mediator = mediator;
        _currentTenant = currentTenant;
        _currentUser = currentUser;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDvvQueryDto dto, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateDvvQueryCommand(
            _currentTenant.TenantId!.Value,
            dto.SsrId,
            dto.QueryNumber,
            dto.CriterionNumber,
            dto.IndicatorNumber,
            dto.QueryText,
            dto.ReceivedAt), ct);

        if (!result.IsSuccess) return BadRequest(result.Error);
        return Ok(new { id = result.Value });
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid? ssrId, [FromQuery] DvvStatus? status, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetDvvQueriesQuery(ssrId, status), ct);
        if (!result.IsSuccess) return BadRequest(result.Error);
        return Ok(result.Value);
    }

    [HttpPost("{id:guid}/respond")]
    public async Task<IActionResult> Respond(Guid id, [FromBody] RespondDvvDto dto, CancellationToken ct)
    {
        var result = await _mediator.Send(new RespondToDvvQueryCommand(
            id, dto.Response, _currentUser.UserId!.Value, dto.SupportingDocUrls), ct);

        if (!result.IsSuccess) return BadRequest(result.Error);
        return NoContent();
    }
}

public record CreateDvvQueryDto(
    Guid SsrId,
    string QueryNumber,
    string CriterionNumber,
    string IndicatorNumber,
    string QueryText,
    DateTime ReceivedAt);

public record RespondDvvDto(string Response, string[]? SupportingDocUrls);
