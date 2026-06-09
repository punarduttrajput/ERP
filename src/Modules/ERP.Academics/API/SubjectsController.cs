using ERP.Academics.Application.Commands;
using ERP.Academics.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Academics.API;

[ApiController]
[Route("api/subjects")]
[Authorize]
public class SubjectsController : ControllerBase
{
    private readonly IMediator _mediator;

    public SubjectsController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSubjectRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(
            new CreateSubjectCommand(request.ProgramId, request.Code, request.Name, request.Credits, request.ContactHoursPerWeek, request.SubjectType), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(new { id = result.Value });
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid? programId = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetSubjectsQuery(programId, page, pageSize), ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetSubjectsQuery(null, 1, int.MaxValue), ct);
        var subject = result.Items.FirstOrDefault(x => x.Id == id);
        if (subject is null)
            return NotFound();
        return Ok(subject);
    }

    [HttpPut("{id:guid}/syllabus")]
    public async Task<IActionResult> UploadSyllabus(Guid id, [FromBody] UploadSyllabusRequest request, CancellationToken ct)
    {
        var bytes = Convert.FromBase64String(request.Base64Content);
        var result = await _mediator.Send(new UploadSyllabusCommand(id, bytes, request.FileName), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(new { url = result.Value });
    }

    [HttpPut("{id:guid}/outcomes")]
    public async Task<IActionResult> SetOutcomes(Guid id, [FromBody] SetCourseOutcomesRequest request, CancellationToken ct)
    {
        var outcomes = request.Outcomes.Select(x => new OutcomeItem(x.Code, x.Description)).ToList();
        var result = await _mediator.Send(new SetCourseOutcomesCommand(id, outcomes), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return NoContent();
    }
}

public record CreateSubjectRequest(Guid ProgramId, string Code, string Name, int Credits, int ContactHoursPerWeek, string SubjectType);
public record UploadSyllabusRequest(string Base64Content, string FileName);
public record SetCourseOutcomesRequest(IReadOnlyList<OutcomeItemRequest> Outcomes);
