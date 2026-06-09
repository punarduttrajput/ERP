using ERP.LMS.Application.Commands;
using ERP.LMS.Application.Queries;
using ERP.LMS.Domain;
using ERP.Shared.Application.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.LMS.API;

[ApiController]
[Route("api/lms/quizzes")]
[Authorize]
public class QuizzesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentTenant _currentTenant;
    private readonly ICurrentUser _currentUser;

    public QuizzesController(IMediator mediator, ICurrentTenant currentTenant, ICurrentUser currentUser)
    {
        _mediator      = mediator;
        _currentTenant = currentTenant;
        _currentUser   = currentUser;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateQuizRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateQuizCommand(
            _currentTenant.TenantId!.Value,
            request.SubjectId,
            request.BatchId,
            request.Title,
            request.Instructions,
            request.DurationMinutes,
            request.MaxAttempts,
            _currentUser.UserId!.Value,
            request.Questions), ct);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(new { id = result.Value });
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid subjectId, [FromQuery] Guid batchId, CancellationToken ct)
    {
        var items = await _mediator.Send(new GetQuizListQuery(subjectId, batchId), ct);
        return Ok(items.Value);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetQuizQuery(id), ct);
        if (!result.IsSuccess)
            return NotFound(new { error = result.Error });
        return Ok(result.Value);
    }

    [HttpPost("{id:guid}/start")]
    public async Task<IActionResult> Start(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new StartQuizAttemptCommand(
            _currentTenant.TenantId!.Value,
            id,
            _currentUser.UserId!.Value), ct);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(new { attemptId = result.Value });
    }

    [HttpPost("attempts/{attemptId:guid}/submit")]
    public async Task<IActionResult> Submit(Guid attemptId, [FromBody] SubmitAttemptRequest request, CancellationToken ct)
    {
        var answers = request.Answers
            .Select(a => new AnswerInput(a.QuestionId, a.AnswerText))
            .ToList();

        var result = await _mediator.Send(new SubmitQuizAttemptCommand(attemptId, answers), ct);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(new { totalMarks = result.Value });
    }
}

public record CreateQuizRequest(
    Guid SubjectId,
    Guid BatchId,
    string Title,
    string? Instructions,
    int DurationMinutes,
    int MaxAttempts,
    IReadOnlyList<QuizQuestionDto> Questions);

public record AnswerInputRequest(Guid QuestionId, string? AnswerText);
public record SubmitAttemptRequest(IReadOnlyList<AnswerInputRequest> Answers);
