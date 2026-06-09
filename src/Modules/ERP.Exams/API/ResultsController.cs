using ERP.Exams.Application.Commands;
using ERP.Exams.Application.Queries;
using ERP.Shared.Application.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Exams.API;

[ApiController]
[Route("api/results")]
[Authorize]
public class ResultsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentTenant _currentTenant;
    private readonly ICurrentUser _currentUser;

    public ResultsController(IMediator mediator, ICurrentTenant currentTenant, ICurrentUser currentUser)
    {
        _mediator = mediator;
        _currentTenant = currentTenant;
        _currentUser = currentUser;
    }

    [HttpPost("internal-marks")]
    public async Task<IActionResult> EnterInternalMarks([FromBody] EnterInternalMarksRequest body, CancellationToken ct)
    {
        var cmd = new EnterInternalMarksCommand(
            _currentTenant.TenantId!.Value,
            body.SemesterId,
            _currentUser.UserId!.Value,
            body.Marks.Select(m => new InternalMarkEntry(m.SubjectId, m.StudentId, m.Marks, m.MaxMarks)).ToList());

        var result = await _mediator.Send(cmd, ct);
        return result.IsSuccess ? Ok(new { upserted = result.Value }) : BadRequest(result.Error);
    }

    [HttpPost("external-marks")]
    public async Task<IActionResult> EnterExternalMarks([FromBody] EnterExternalMarksRequest body, CancellationToken ct)
    {
        var cmd = new EnterExternalMarksCommand(
            _currentTenant.TenantId!.Value,
            body.SemesterId,
            _currentUser.UserId!.Value,
            body.Marks.Select(m => new ExternalMarkEntry(m.ExamScheduleId, m.StudentId, m.Marks, m.IsAbsent)).ToList());

        var result = await _mediator.Send(cmd, ct);
        return result.IsSuccess ? Ok(new { upserted = result.Value }) : BadRequest(result.Error);
    }

    [HttpPost("{semesterId:guid}/publish")]
    public async Task<IActionResult> PublishResults(Guid semesterId, CancellationToken ct)
    {
        var cmd = new PublishResultsCommand(_currentTenant.TenantId!.Value, semesterId);
        var result = await _mediator.Send(cmd, ct);
        return result.IsSuccess ? Ok(new { publishedStudents = result.Value }) : BadRequest(result.Error);
    }

    [HttpGet("students/{studentId:guid}/semester/{semesterId:guid}")]
    public async Task<IActionResult> GetStudentResult(Guid studentId, Guid semesterId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetStudentResultQuery(studentId, semesterId), ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    [HttpGet("students/{studentId:guid}/semester/{semesterId:guid}/grade-card")]
    public async Task<IActionResult> GetGradeCard(Guid studentId, Guid semesterId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetGradeCardQuery(studentId, semesterId), ct);

        if (!result.IsSuccess)
            return NotFound(result.Error);

        Response.Headers["Content-Disposition"] = "attachment; filename=grade-card.pdf";
        return File(result.Value!, "application/pdf");
    }

    [HttpPost("arrear")]
    public async Task<IActionResult> RegisterArrear([FromBody] RegisterArrearRequest body, CancellationToken ct)
    {
        var cmd = new RegisterArrearCommand(
            _currentTenant.TenantId!.Value,
            body.StudentId,
            body.SubjectId,
            body.SemesterId,
            body.ExamSemesterId);

        var result = await _mediator.Send(cmd, ct);
        return result.IsSuccess ? Ok(new { registrationId = result.Value }) : BadRequest(result.Error);
    }
}

public record InternalMarkEntryRequest(
    Guid SubjectId,
    Guid StudentId,
    decimal Marks,
    decimal MaxMarks);

public record EnterInternalMarksRequest(
    Guid SemesterId,
    IReadOnlyList<InternalMarkEntryRequest> Marks);

public record ExternalMarkEntryRequest(
    Guid ExamScheduleId,
    Guid StudentId,
    decimal Marks,
    bool IsAbsent);

public record EnterExternalMarksRequest(
    Guid SemesterId,
    IReadOnlyList<ExternalMarkEntryRequest> Marks);

public record RegisterArrearRequest(
    Guid StudentId,
    Guid SubjectId,
    Guid SemesterId,
    Guid ExamSemesterId);
