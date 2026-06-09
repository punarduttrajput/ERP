using ERP.Library.Application.Commands;
using ERP.Library.Application.Queries;
using ERP.Library.Domain;
using ERP.Shared.Application.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Library.API;

[ApiController]
[Route("api/library/circulation")]
[Authorize]
public class CirculationController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentTenant _currentTenant;

    public CirculationController(IMediator mediator, ICurrentTenant currentTenant)
    {
        _mediator = mediator;
        _currentTenant = currentTenant;
    }

    [HttpPost("issue")]
    public async Task<IActionResult> IssueBook([FromBody] IssueBookRequest request, CancellationToken cancellationToken)
    {
        var command = new IssueBookCommand(
            request.Barcode, request.MemberId, request.MemberType,
            request.MemberName, request.IssuedAt ?? DateTime.UtcNow,
            _currentTenant.TenantId ?? Guid.Empty
        );

        var result = await _mediator.Send(command, cancellationToken);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(result.Value);
    }

    [HttpPost("return")]
    public async Task<IActionResult> ReturnBook([FromBody] ReturnBookRequest request, CancellationToken cancellationToken)
    {
        var command = new ReturnBookCommand(
            request.Barcode, request.ReturnedAt ?? DateTime.UtcNow,
            _currentTenant.TenantId ?? Guid.Empty
        );

        var result = await _mediator.Send(command, cancellationToken);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(result.Value);
    }

    [HttpGet("issues")]
    public async Task<IActionResult> GetIssues(
        [FromQuery] Guid? memberId,
        [FromQuery] IssueStatus? status,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetIssuedBooksQuery(memberId, status), cancellationToken);
        return Ok(result);
    }

    [HttpGet("overdue")]
    public async Task<IActionResult> GetOverdue(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetOverdueIssuedBooksQuery(), cancellationToken);
        return Ok(result);
    }
}

public record IssueBookRequest(
    string Barcode,
    Guid MemberId,
    MemberType MemberType,
    string MemberName,
    DateTime? IssuedAt
);

public record ReturnBookRequest(
    string Barcode,
    DateTime? ReturnedAt
);
