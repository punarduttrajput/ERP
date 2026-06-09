using ERP.Academics.Application.Commands;
using ERP.Academics.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Academics.API;

[ApiController]
[Route("api/academic-calendar")]
[Authorize]
public class AcademicCalendarController : ControllerBase
{
    private readonly IMediator _mediator;

    public AcademicCalendarController(IMediator mediator) => _mediator = mediator;

    [HttpPost("years")]
    public async Task<IActionResult> CreateYear([FromBody] CreateAcademicYearRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(
            new CreateAcademicYearCommand(request.Label, request.StartDate, request.EndDate, request.IsCurrent), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(new { id = result.Value });
    }

    [HttpGet("years")]
    public async Task<IActionResult> ListYears([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetAcademicYearsQuery(page, pageSize), ct);
        return Ok(result);
    }

    [HttpPost("semesters")]
    public async Task<IActionResult> CreateSemester([FromBody] CreateSemesterRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(
            new CreateSemesterCommand(request.AcademicYearId, request.Number, request.Label, request.StartDate, request.EndDate, request.IsCurrent), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(new { id = result.Value });
    }

    [HttpGet("semesters")]
    public async Task<IActionResult> ListSemesters([FromQuery] Guid? academicYearId = null, [FromQuery] bool? isCurrent = null, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetSemestersQuery(academicYearId, isCurrent), ct);
        return Ok(result);
    }

    [HttpPost("batches")]
    public async Task<IActionResult> CreateBatch([FromBody] CreateBatchRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(
            new CreateBatchCommand(request.ProgramId, request.AcademicYearId, request.Name, request.AdmissionYear), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(new { id = result.Value });
    }

    [HttpGet("batches")]
    public async Task<IActionResult> ListBatches([FromQuery] Guid? programId = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetBatchesQuery(programId, page, pageSize), ct);
        return Ok(result);
    }
}

public record CreateAcademicYearRequest(string Label, DateOnly StartDate, DateOnly EndDate, bool IsCurrent);
public record CreateSemesterRequest(Guid AcademicYearId, int Number, string Label, DateOnly StartDate, DateOnly EndDate, bool IsCurrent);
public record CreateBatchRequest(Guid ProgramId, Guid AcademicYearId, string Name, int AdmissionYear);
