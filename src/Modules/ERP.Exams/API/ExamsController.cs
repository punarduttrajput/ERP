using ERP.Exams.Application.Commands;
using ERP.Exams.Application.Queries;
using ERP.Shared.Application.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Exams.API;

[ApiController]
[Route("api/exams")]
[Authorize]
public class ExamsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentTenant _currentTenant;
    private readonly ICurrentUser _currentUser;

    public ExamsController(IMediator mediator, ICurrentTenant currentTenant, ICurrentUser currentUser)
    {
        _mediator = mediator;
        _currentTenant = currentTenant;
        _currentUser = currentUser;
    }

    [HttpPost("schedules")]
    public async Task<IActionResult> CreateSchedule([FromBody] CreateExamScheduleRequest body, CancellationToken ct)
    {
        var cmd = new CreateExamScheduleCommand(
            _currentTenant.TenantId!.Value,
            body.SemesterId,
            body.SubjectId,
            body.SubjectName,
            body.ExamDate,
            body.StartTime,
            body.EndTime,
            body.Venue,
            body.MaxMarks,
            body.PassingMarks);

        var result = await _mediator.Send(cmd, ct);
        return result.IsSuccess ? Ok(new { scheduleId = result.Value }) : BadRequest(result.Error);
    }

    [HttpGet("schedules")]
    public async Task<IActionResult> GetSchedules([FromQuery] Guid semesterId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetExamScheduleQuery(semesterId), ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpPost("schedules/{id:guid}/seating")]
    public async Task<IActionResult> GenerateSeating(Guid id, [FromBody] GenerateSeatingRequest body, CancellationToken ct)
    {
        var cmd = new GenerateSeatingPlanCommand(
            _currentTenant.TenantId!.Value,
            id,
            body.SeatingOrder,
            body.Students.Select(s => new StudentSeatInfo(s.StudentId, s.RollNumber)).ToList());

        var result = await _mediator.Send(cmd, ct);
        return result.IsSuccess ? Ok(new { allocatedSeats = result.Value }) : BadRequest(result.Error);
    }

    [HttpGet("schedules/{id:guid}/seating")]
    public async Task<IActionResult> GetSeating(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetSeatingPlanQuery(id), ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpGet("hall-ticket")]
    public async Task<IActionResult> GetHallTicket([FromQuery] Guid studentId, [FromQuery] Guid scheduleId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetHallTicketQuery(studentId, scheduleId), ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpPost("grading-schemes")]
    public async Task<IActionResult> CreateGradingScheme([FromBody] CreateGradingSchemeRequest body, CancellationToken ct)
    {
        var cmd = new CreateGradingSchemeCommand(
            _currentTenant.TenantId!.Value,
            body.Name,
            body.IsDefault,
            body.Rules.Select(r => new GradeRuleDto(r.MinMarks, r.MaxMarks, r.GradeLetter, r.GradePoints)).ToList());

        var result = await _mediator.Send(cmd, ct);
        return result.IsSuccess ? Ok(new { schemeId = result.Value }) : BadRequest(result.Error);
    }
}

public record CreateExamScheduleRequest(
    Guid SemesterId,
    Guid SubjectId,
    string SubjectName,
    DateOnly ExamDate,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string Venue,
    int MaxMarks,
    int PassingMarks);

public record StudentSeatRequest(Guid StudentId, string RollNumber);

public record GenerateSeatingRequest(
    string SeatingOrder,
    IReadOnlyList<StudentSeatRequest> Students);

public record GradeRuleRequest(
    decimal MinMarks,
    decimal MaxMarks,
    string GradeLetter,
    decimal GradePoints);

public record CreateGradingSchemeRequest(
    string Name,
    bool IsDefault,
    IReadOnlyList<GradeRuleRequest> Rules);
