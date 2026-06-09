using ERP.Attendance.Application.Commands;
using ERP.Attendance.Application.Queries;
using ERP.Shared.Application.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Attendance.API;

[ApiController]
[Route("api/attendance")]
[Authorize]
public class AttendanceController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentTenant _currentTenant;
    private readonly ICurrentUser _currentUser;

    public AttendanceController(IMediator mediator, ICurrentTenant currentTenant, ICurrentUser currentUser)
    {
        _mediator = mediator;
        _currentTenant = currentTenant;
        _currentUser = currentUser;
    }

    [HttpPost("sessions")]
    public async Task<IActionResult> CreateSession([FromBody] CreateSessionRequest body, CancellationToken ct)
    {
        var cmd = new CreateSessionCommand(
            _currentTenant.TenantId!.Value,
            body.SubjectId,
            body.BatchId,
            body.SemesterId,
            body.FacultyUserId,
            body.SessionDate,
            body.PeriodNumber,
            body.StudentIds);

        var result = await _mediator.Send(cmd, ct);
        return result.IsSuccess ? Ok(new { sessionId = result.Value }) : BadRequest(result.Error);
    }

    [HttpPost("sessions/{id:guid}/mark")]
    public async Task<IActionResult> MarkAttendance(Guid id, [FromBody] MarkAttendanceRequest body, CancellationToken ct)
    {
        var cmd = new MarkAttendanceCommand(
            _currentTenant.TenantId!.Value,
            id,
            body.Marks.Select(m => new AttendanceMark(m.StudentId, m.Status)).ToList());

        var result = await _mediator.Send(cmd, ct);
        return result.IsSuccess ? Ok() : BadRequest(result.Error);
    }

    [HttpPost("sessions/{id:guid}/qr")]
    public async Task<IActionResult> GenerateQr(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GenerateQrCommand(id), ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpPost("qr/submit")]
    public async Task<IActionResult> SubmitQr([FromBody] SubmitQrRequest body, CancellationToken ct)
    {
        var result = await _mediator.Send(new SubmitQrAttendanceCommand(body.QrToken, body.StudentId), ct);
        return result.IsSuccess ? Ok() : BadRequest(result.Error);
    }

    [HttpGet("sessions/{id:guid}")]
    public async Task<IActionResult> GetSession(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetSessionAttendanceQuery(id), ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    [HttpGet("students/{studentId:guid}/summary")]
    public async Task<IActionResult> GetStudentSummary(Guid studentId, [FromQuery] Guid semesterId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetStudentAttendanceSummaryQuery(_currentTenant.TenantId!.Value, studentId, semesterId), ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpGet("batches/{batchId:guid}/report")]
    public async Task<IActionResult> GetBatchReport(Guid batchId, [FromQuery] Guid semesterId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetBatchAttendanceReportQuery(_currentTenant.TenantId!.Value, batchId, semesterId), ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpPost("regularization")]
    public async Task<IActionResult> SubmitRegularization([FromBody] SubmitRegularizationRequest body, CancellationToken ct)
    {
        var cmd = new SubmitRegularizationCommand(
            _currentTenant.TenantId!.Value,
            body.SessionId,
            body.StudentId,
            body.Reason);

        var result = await _mediator.Send(cmd, ct);
        return result.IsSuccess ? Ok(new { requestId = result.Value }) : BadRequest(result.Error);
    }

    [HttpPatch("regularization/{id:guid}")]
    public async Task<IActionResult> ReviewRegularization(Guid id, [FromBody] ReviewRegularizationRequest body, CancellationToken ct)
    {
        var cmd = new ReviewRegularizationCommand(id, _currentUser.UserId!.Value, body.Approved, body.Remark);
        var result = await _mediator.Send(cmd, ct);
        return result.IsSuccess ? Ok() : BadRequest(result.Error);
    }
}

public record CreateSessionRequest(
    Guid SubjectId,
    Guid BatchId,
    Guid SemesterId,
    Guid FacultyUserId,
    DateOnly SessionDate,
    int PeriodNumber,
    IReadOnlyList<Guid> StudentIds);

public record MarkAttendanceRequestItem(Guid StudentId, ERP.Attendance.Domain.AttendanceStatus Status);

public record MarkAttendanceRequest(IReadOnlyList<MarkAttendanceRequestItem> Marks);

public record SubmitQrRequest(string QrToken, Guid StudentId);

public record SubmitRegularizationRequest(Guid SessionId, Guid StudentId, string Reason);

public record ReviewRegularizationRequest(bool Approved, string? Remark);
