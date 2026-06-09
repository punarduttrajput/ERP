using ERP.RBAC.Application.Commands;
using ERP.RBAC.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.RBAC.API;

[ApiController]
[Route("api/roles")]
[Authorize]
public class RolesController : ControllerBase
{
    private readonly IMediator _mediator;

    public RolesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetRolesQuery(page, pageSize), cancellationToken);
        return Ok(new { success = true, data = result.Value, traceId = HttpContext.TraceIdentifier });
    }

    [HttpPost]
    [Authorize(Policy = "roles:write")]
    public async Task<IActionResult> Create([FromBody] CreateRoleDto dto, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CreateRoleCommand(dto.Name, dto.Description), cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(new { success = false, message = result.Error, traceId = HttpContext.TraceIdentifier });

        return Ok(new { success = true, data = new { id = result.Value }, traceId = HttpContext.TraceIdentifier });
    }

    [HttpPost("{roleId:guid}/permissions")]
    [Authorize(Policy = "roles:write")]
    public async Task<IActionResult> AssignPermission(Guid roleId, [FromBody] AssignPermissionDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new AssignPermissionCommand(roleId, dto.PermissionId), cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(new { success = false, message = result.Error, traceId = HttpContext.TraceIdentifier });

        return Ok(new { success = true, data = new { }, traceId = HttpContext.TraceIdentifier });
    }
}

public record CreateRoleDto(string Name, string? Description);
public record AssignPermissionDto(Guid PermissionId);
