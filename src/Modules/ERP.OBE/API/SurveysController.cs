using ERP.OBE.Application.Commands;
using ERP.OBE.Infrastructure;
using ERP.Shared.Application.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.OBE.API;

[ApiController]
[Route("api/obe/surveys")]
[Authorize]
public class SurveysController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentTenant _currentTenant;
    private readonly IObeDbContext _db;

    public SurveysController(IMediator mediator, ICurrentTenant currentTenant, IObeDbContext db)
    {
        _mediator = mediator;
        _currentTenant = currentTenant;
        _db = db;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSurveyDto dto, CancellationToken ct)
    {
        var result = await _mediator.Send(
            new CreateSurveyCommand(
                _currentTenant.TenantId!.Value,
                dto.SubjectId,
                dto.SemesterId,
                dto.AcademicYear,
                dto.Title,
                dto.Questions.Select(q => new SurveyQuestionItem(q.CourseOutcomeCode, q.QuestionText, q.OrderIndex)).ToList()), ct);
        if (!result.IsSuccess) return BadRequest(result.Error);
        return CreatedAtAction(nameof(Get), new { id = result.Value }, new { id = result.Value });
    }

    [HttpPost("{id:guid}/publish")]
    public async Task<IActionResult> Publish(Guid id, CancellationToken ct)
    {
        var survey = await _db.IndirectSurveys.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (survey is null) return NotFound();
        survey.IsPublished = true;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/responses")]
    public async Task<IActionResult> SubmitResponse(Guid id, [FromBody] SubmitResponseDto dto, CancellationToken ct)
    {
        var result = await _mediator.Send(
            new SubmitSurveyResponseCommand(
                _currentTenant.TenantId!.Value,
                id,
                dto.StudentId,
                dto.Answers.Select(a => new SurveyAnswerItem(a.QuestionId, a.Score)).ToList()), ct);
        if (!result.IsSuccess) return BadRequest(result.Error);
        return NoContent();
    }

    [HttpPost("{id:guid}/compute")]
    public async Task<IActionResult> Compute(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new ComputeIndirectAttainmentCommand(_currentTenant.TenantId!.Value, id), ct);
        if (!result.IsSuccess) return BadRequest(result.Error);
        return NoContent();
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var survey = await _db.IndirectSurveys
            .Include(x => x.Questions)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (survey is null) return NotFound();
        return Ok(survey);
    }
}

public record CreateSurveyDto(Guid SubjectId, Guid SemesterId, int AcademicYear, string Title, IReadOnlyList<SurveyQuestionDto> Questions);
public record SurveyQuestionDto(string CourseOutcomeCode, string QuestionText, int OrderIndex);
public record SubmitResponseDto(Guid StudentId, IReadOnlyList<SurveyAnswerDto> Answers);
public record SurveyAnswerDto(Guid QuestionId, int Score);
