using ERP.LMS.Application.Commands;
using ERP.LMS.Application.Queries;
using ERP.Shared.Application.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.LMS.API;

[ApiController]
[Route("api/lms/announcements")]
[Authorize]
public class AnnouncementsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentTenant _currentTenant;
    private readonly ICurrentUser _currentUser;

    public AnnouncementsController(IMediator mediator, ICurrentTenant currentTenant, ICurrentUser currentUser)
    {
        _mediator      = mediator;
        _currentTenant = currentTenant;
        _currentUser   = currentUser;
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] PostAnnouncementRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new PostAnnouncementCommand(
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
    public async Task<IActionResult> List([FromQuery] Guid subjectId, [FromQuery] Guid batchId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetAnnouncementsQuery(subjectId, batchId), ct);
        return Ok(result.Value);
    }
}

public record PostAnnouncementRequest(Guid SubjectId, Guid BatchId, string Title, string Body);
