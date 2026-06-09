using ERP.RBAC.Application.Commands;
using ERP.Users.API.Dtos;
using ERP.Users.Application.Commands;
using ERP.Users.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Users.API;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null, [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetUsersQuery(page, pageSize, search, isActive), cancellationToken);
        return Ok(new { success = true, data = result.Value, traceId = HttpContext.TraceIdentifier });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetUserByIdQuery(id), cancellationToken);
        if (!result.IsSuccess)
            return NotFound(new { success = false, message = result.Error, traceId = HttpContext.TraceIdentifier });

        return Ok(new { success = true, data = result.Value, traceId = HttpContext.TraceIdentifier });
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Create([FromBody] CreateUserDto dto, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new CreateUserCommand(dto.Email, dto.Password, dto.FirstName, dto.LastName, dto.PhoneNumber, dto.Department, dto.JobTitle),
            cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(new { success = false, message = result.Error, traceId = HttpContext.TraceIdentifier });

        return CreatedAtAction(nameof(GetById), new { id = result.Value },
            new { success = true, data = new { id = result.Value }, traceId = HttpContext.TraceIdentifier });
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "users:write")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserDto dto, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new UpdateUserCommand(id, dto.FirstName, dto.LastName, dto.PhoneNumber, dto.Department, dto.JobTitle, dto.AvatarUrl),
            cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(new { success = false, message = result.Error, traceId = HttpContext.TraceIdentifier });

        return Ok(new { success = true, data = new { }, traceId = HttpContext.TraceIdentifier });
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "users:write")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new DeactivateUserCommand(id), cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(new { success = false, message = result.Error, traceId = HttpContext.TraceIdentifier });

        return Ok(new { success = true, data = new { }, traceId = HttpContext.TraceIdentifier });
    }

    [HttpPost("{userId:guid}/roles")]
    [Authorize(Policy = "users:write")]
    public async Task<IActionResult> AssignRole(Guid userId, [FromBody] AssignRoleDto dto, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new AssignUserRoleCommand(userId, dto.RoleId), cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(new { success = false, message = result.Error, traceId = HttpContext.TraceIdentifier });

        return Ok(new { success = true, data = new { }, traceId = HttpContext.TraceIdentifier });
    }
}

public record UpdateUserDto(string? FirstName, string? LastName, string? PhoneNumber, string? Department, string? JobTitle, string? AvatarUrl);
public record AssignRoleDto(Guid RoleId);
