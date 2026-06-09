using ERP.LMS.Application.Commands;
using ERP.LMS.Application.Queries;
using ERP.Shared.Application.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.LMS.API;

[ApiController]
[Route("api/lms/assignments")]
[Authorize]
public class AssignmentsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentTenant _currentTenant;
    private readonly ICurrentUser _currentUser;

    public AssignmentsController(IMediator mediator, ICurrentTenant currentTenant, ICurrentUser currentUser)
    {
        _mediator      = mediator;
        _currentTenant = currentTenant;
        _currentUser   = currentUser;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAssignmentRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateAssignmentCommand(
            _currentTenant.TenantId!.Value,
            request.SubjectId,
            request.BatchId,
            request.Title,
            request.Description,
            request.DueDate,
            request.MaxMarks,
            _currentUser.UserId!.Value), ct);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(new { id = result.Value });
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid subjectId, [FromQuery] Guid batchId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetAssignmentsQuery(subjectId, batchId), ct);
        return Ok(result.Value);
    }

    [HttpPost("{id:guid}/submit")]
    public async Task<IActionResult> Submit(Guid id, [FromBody] SubmitAssignmentRequest request, CancellationToken ct)
    {
        var fileBytes = Convert.FromBase64String(request.FileBytesBase64);
        var result = await _mediator.Send(new SubmitAssignmentCommand(
            _currentTenant.TenantId!.Value,
            id,
            _currentUser.UserId!.Value,
            fileBytes,
            request.FileName), ct);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(new { id = result.Value });
    }

    [HttpGet("{id:guid}/submissions")]
    public async Task<IActionResult> GetSubmissions(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetSubmissionsQuery(id), ct);
        return Ok(result.Value);
    }

    [HttpPatch("submissions/{submissionId:guid}/grade")]
    public async Task<IActionResult> Grade(Guid submissionId, [FromBody] GradeRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new GradeAssignmentCommand(
            submissionId,
            request.MarksAwarded,
            request.FacultyFeedback,
            _currentUser.UserId!.Value), ct);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return NoContent();
    }
}

public record CreateAssignmentRequest(Guid SubjectId, Guid BatchId, string Title, string Description, DateTime DueDate, decimal MaxMarks);
public record SubmitAssignmentRequest(string FileBytesBase64, string FileName);
public record GradeRequest(decimal MarksAwarded, string? FacultyFeedback);
