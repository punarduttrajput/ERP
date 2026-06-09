using ERP.Library.Application.Commands;
using ERP.Library.Application.Queries;
using ERP.Library.Domain;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Library.API;

[ApiController]
[Route("api/library/fines")]
[Authorize]
public class FinesController : ControllerBase
{
    private readonly IMediator _mediator;

    public FinesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetFines(
        [FromQuery] Guid? memberId,
        [FromQuery] FineStatus? status,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetFinesQuery(memberId, status), cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/collect")]
    public async Task<IActionResult> CollectFine(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CollectFineCommand(id, DateTime.UtcNow), cancellationToken);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok();
    }

    [HttpPost("{id:guid}/waive")]
    public async Task<IActionResult> WaiveFine(Guid id, [FromBody] WaiveFineRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new WaiveFineCommand(id, request.WaivedBy, request.Reason), cancellationToken);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok();
    }
}

public record WaiveFineRequest(Guid WaivedBy, string Reason);
