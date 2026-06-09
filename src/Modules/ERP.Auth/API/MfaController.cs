using ERP.Auth.Application.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Auth.API;

[ApiController]
[Route("api/auth/mfa")]
public class MfaController : ControllerBase
{
    private readonly IMediator _mediator;

    public MfaController(IMediator mediator) => _mediator = mediator;

    /// <summary>Step 1: generate TOTP secret + QR code URI for the authenticator app.</summary>
    [HttpPost("enable")]
    [Authorize]
    public async Task<IActionResult> Enable(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var result = await _mediator.Send(new EnableMfaCommand(userId), cancellationToken);
        return result.IsSuccess
            ? Ok(new { success = true, data = result.Value })
            : BadRequest(new { success = false, message = result.Error });
    }

    /// <summary>Step 2: verify first TOTP code to activate MFA. Returns one-time recovery codes.</summary>
    [HttpPost("confirm")]
    [Authorize]
    public async Task<IActionResult> Confirm([FromBody] ConfirmMfaRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var result = await _mediator.Send(new ConfirmMfaCommand(userId, request.TotpCode), cancellationToken);
        return result.IsSuccess
            ? Ok(new { success = true, data = result.Value })
            : BadRequest(new { success = false, message = result.Error });
    }

    /// <summary>Disable MFA. Requires a valid TOTP code to confirm intent.</summary>
    [HttpPost("disable")]
    [Authorize]
    public async Task<IActionResult> Disable([FromBody] DisableMfaRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var result = await _mediator.Send(new DisableMfaCommand(userId, request.TotpCode), cancellationToken);
        return result.IsSuccess
            ? Ok(new { success = true })
            : BadRequest(new { success = false, message = result.Error });
    }

    /// <summary>Complete login after MFA challenge. Accepts TOTP code or a recovery code.</summary>
    [HttpPost("verify-login")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyLogin([FromBody] VerifyMfaLoginRequest request, CancellationToken cancellationToken)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _mediator.Send(
            new VerifyMfaLoginCommand(request.MfaChallengeToken, request.Code, ip), cancellationToken);

        return result.IsSuccess
            ? Ok(new { success = true, data = result.Value })
            : Unauthorized(new { success = false, message = result.Error });
    }

    /// <summary>Send OTP to mobile (SMS fallback for TOTP).</summary>
    [HttpPost("otp/send")]
    [AllowAnonymous]
    public async Task<IActionResult> SendOtp([FromBody] SendOtpRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new SendOtpCommand(request.MobileNumber), cancellationToken);
        return result.IsSuccess
            ? Ok(new { success = true, message = "OTP sent." })
            : BadRequest(new { success = false, message = result.Error });
    }

    /// <summary>Verify SMS OTP and receive full tokens.</summary>
    [HttpPost("otp/verify")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest request, CancellationToken cancellationToken)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _mediator.Send(new VerifyOtpCommand(request.MobileNumber, request.Otp, ip), cancellationToken);
        return result.IsSuccess
            ? Ok(new { success = true, data = result.Value })
            : Unauthorized(new { success = false, message = result.Error });
    }

    private Guid GetUserId()
    {
        var sub = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
               ?? User.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }
}

public record ConfirmMfaRequest(string TotpCode);
public record DisableMfaRequest(string TotpCode);
public record VerifyMfaLoginRequest(string MfaChallengeToken, string Code);
public record SendOtpRequest(string MobileNumber);
public record VerifyOtpRequest(string MobileNumber, string Otp);
