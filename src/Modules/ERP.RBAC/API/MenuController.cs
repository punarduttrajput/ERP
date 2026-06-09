using ERP.RBAC.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ERP.RBAC.API;

[ApiController]
[Route("api/menu")]
[Authorize]
public class MenuController : ControllerBase
{
    private readonly IMediator _mediator;

    public MenuController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetMenu(CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;

        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { success = false, message = "Invalid user identity", traceId = HttpContext.TraceIdentifier });

        var result = await _mediator.Send(new GetMenuForUserQuery(userId), cancellationToken);

        return Ok(new { success = true, data = result.Value, traceId = HttpContext.TraceIdentifier });
    }
}
