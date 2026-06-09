using ERP.Auth.API.Dtos;
using ERP.Auth.Application.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Auth.API;

[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _mediator.Send(new LoginCommand(request.Email, request.Password, ipAddress), cancellationToken);

        if (!result.IsSuccess)
            return Unauthorized(new { success = false, message = result.Error, traceId = HttpContext.TraceIdentifier });

        return Ok(new { success = true, data = result.Value, traceId = HttpContext.TraceIdentifier });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _mediator.Send(new RefreshTokenCommand(request.Token, ipAddress), cancellationToken);

        if (!result.IsSuccess)
            return Unauthorized(new { success = false, message = result.Error, traceId = HttpContext.TraceIdentifier });

        return Ok(new { success = true, data = result.Value, traceId = HttpContext.TraceIdentifier });
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new LogoutCommand(request.Token), cancellationToken);

        if (!result.IsSuccess)
            return BadRequest(new { success = false, message = result.Error, traceId = HttpContext.TraceIdentifier });

        return Ok(new { success = true, data = new { }, traceId = HttpContext.TraceIdentifier });
    }
}
