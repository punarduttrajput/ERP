using ERP.LMS.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.LMS.API;

[ApiController]
[Route("api/lms/progress")]
[Authorize]
public class ProgressController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProgressController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] Guid studentId, [FromQuery] Guid subjectId, [FromQuery] Guid batchId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetStudentProgressQuery(studentId, subjectId, batchId), ct);
        return Ok(result.Value);
    }
}
