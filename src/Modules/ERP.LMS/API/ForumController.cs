using ERP.LMS.Application.Commands;
using ERP.LMS.Application.Queries;
using ERP.Shared.Application.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.LMS.API;

[ApiController]
[Route("api/lms/forum")]
[Authorize]
public class ForumController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentTenant _currentTenant;
    private readonly ICurrentUser _currentUser;

    public ForumController(IMediator mediator, ICurrentTenant currentTenant, ICurrentUser currentUser)
    {
        _mediator      = mediator;
        _currentTenant = currentTenant;
        _currentUser   = currentUser;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateThreadRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateForumThreadCommand(
            _currentTenant.TenantId!.Value,
            request.SubjectId,
            request.BatchId,
            request.Title,
            request.Body,
            _currentUser.UserId!.Value), ct);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(new { id = result.Value });
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid subjectId, [FromQuery] Guid batchId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetForumThreadsQuery(subjectId, batchId, page, pageSize), ct);
        return Ok(result.Value);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetForumThreadDetailQuery(id), ct);
        if (!result.IsSuccess)
            return NotFound(new { error = result.Error });
        return Ok(result.Value);
    }

    [HttpPost("{id:guid}/reply")]
    public async Task<IActionResult> Reply(Guid id, [FromBody] ReplyRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new ReplyToThreadCommand(
            _currentTenant.TenantId!.Value,
            id,
            _currentUser.UserId!.Value,
            request.Body), ct);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(new { id = result.Value });
    }
}

public record CreateThreadRequest(Guid SubjectId, Guid BatchId, string Title, string Body);
public record ReplyRequest(string Body);
