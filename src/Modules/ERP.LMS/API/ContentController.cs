using ERP.LMS.Application.Commands;
using ERP.LMS.Application.Queries;
using ERP.LMS.Domain;
using ERP.Shared.Application.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.LMS.API;

[ApiController]
[Route("api/lms/content")]
[Authorize]
public class ContentController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentTenant _currentTenant;
    private readonly ICurrentUser _currentUser;

    public ContentController(IMediator mediator, ICurrentTenant currentTenant, ICurrentUser currentUser)
    {
        _mediator      = mediator;
        _currentTenant = currentTenant;
        _currentUser   = currentUser;
    }

    [HttpPost]
    public async Task<IActionResult> Upload([FromBody] UploadContentRequest request, CancellationToken ct)
    {
        byte[]? fileBytes = null;
        if (!string.IsNullOrWhiteSpace(request.FileBytesBase64))
            fileBytes = Convert.FromBase64String(request.FileBytesBase64);

        var result = await _mediator.Send(new UploadContentCommand(
            _currentTenant.TenantId!.Value,
            request.SubjectId,
            request.BatchId,
            request.Title,
            request.Description,
            request.ContentType,
            fileBytes,
            request.FileName,
            request.ExternalUrl,
            request.OrderIndex,
            _currentUser.UserId!.Value), ct);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(new { id = result.Value });
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid subjectId, [FromQuery] Guid batchId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetCourseContentQuery(subjectId, batchId), ct);
        return Ok(result.Value);
    }

    [HttpPost("{id:guid}/viewed")]
    public async Task<IActionResult> MarkViewed(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new MarkContentViewedCommand(
            _currentTenant.TenantId!.Value,
            _currentUser.UserId!.Value,
            id), ct);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return NoContent();
    }
}

public record UploadContentRequest(
    Guid SubjectId,
    Guid BatchId,
    string Title,
    string? Description,
    ContentType ContentType,
    string? FileBytesBase64,
    string? FileName,
    string? ExternalUrl,
    int OrderIndex);
