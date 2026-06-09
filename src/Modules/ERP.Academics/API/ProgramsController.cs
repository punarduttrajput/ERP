using ERP.Academics.Application.Commands;
using ERP.Academics.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Academics.API;

[ApiController]
[Route("api/programs")]
[Authorize]
public class ProgramsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProgramsController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProgramRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(
            new CreateProgramCommand(request.DepartmentId, request.Code, request.Name, request.DurationYears, request.DegreeType), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(new { id = result.Value });
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid? departmentId = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetProgramsQuery(departmentId, page, pageSize), ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}/curriculum")]
    public async Task<IActionResult> GetCurriculum(Guid id, [FromQuery] int? semesterNumber = null, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetCurriculumQuery(id, semesterNumber), ct);
        return Ok(result);
    }

    [HttpPost("{id:guid}/curriculum")]
    public async Task<IActionResult> MapCurriculum(Guid id, [FromBody] MapCurriculumRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new MapCurriculumCommand(id, request.SemesterNumber, request.SubjectId, request.IsElective), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(new { id = result.Value });
    }

    [HttpGet("{id:guid}/outcomes")]
    public async Task<IActionResult> GetOutcomes(Guid id, CancellationToken ct)
    {
        var pos = await _mediator.Send(new GetProgramOutcomesQuery(id), ct);
        return Ok(pos);
    }

    [HttpPut("{id:guid}/outcomes")]
    public async Task<IActionResult> SetOutcomes(Guid id, [FromBody] SetProgramOutcomesRequest request, CancellationToken ct)
    {
        var pos = request.POs.Select(x => new OutcomeItem(x.Code, x.Description)).ToList();
        var psos = request.PSOs.Select(x => new OutcomeItem(x.Code, x.Description)).ToList();
        var result = await _mediator.Send(new SetProgramOutcomesCommand(id, pos, psos), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return NoContent();
    }
}

public record CreateProgramRequest(Guid DepartmentId, string Code, string Name, int DurationYears, string DegreeType);
public record MapCurriculumRequest(int SemesterNumber, Guid SubjectId, bool IsElective);
public record OutcomeItemRequest(string Code, string Description);
public record SetProgramOutcomesRequest(IReadOnlyList<OutcomeItemRequest> POs, IReadOnlyList<OutcomeItemRequest> PSOs);
