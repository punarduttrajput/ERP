using ERP.Academics.Application.Commands;
using ERP.Academics.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Academics.API;

[ApiController]
[Route("api/departments")]
[Authorize]
public class DepartmentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public DepartmentsController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDepartmentRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateDepartmentCommand(request.Code, request.Name, request.HeadOfDepartmentUserId), ct);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });
        return Ok(new { id = result.Value });
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetDepartmentsQuery(page, pageSize), ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}/programs")]
    public async Task<IActionResult> GetPrograms(Guid id, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetProgramsQuery(id, page, pageSize), ct);
        return Ok(result);
    }
}

public record CreateDepartmentRequest(string Code, string Name, Guid? HeadOfDepartmentUserId);
