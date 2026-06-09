using ERP.Attendance.Application.Commands;
using ERP.Shared.Application.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace ERP.Attendance.API;

[ApiController]
[Route("api/attendance/biometric")]
public class BiometricController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IConfiguration _configuration;
    private readonly ICurrentTenant _currentTenant;

    public BiometricController(IMediator mediator, IConfiguration configuration, ICurrentTenant currentTenant)
    {
        _mediator = mediator;
        _configuration = configuration;
        _currentTenant = currentTenant;
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook([FromBody] BiometricWebhookPayload payload, CancellationToken ct)
    {
        var expectedApiKey = _configuration["Biometric:ApiKey"];

        if (!Request.Headers.TryGetValue("X-Api-Key", out var providedKey) || providedKey != expectedApiKey)
            return Unauthorized();

        var cmd = new SubmitBiometricLogCommand(
            _currentTenant.TenantId ?? Guid.Empty,
            payload.DeviceId,
            payload.BiometricId,
            payload.Timestamp);

        var result = await _mediator.Send(cmd, ct);
        return result.IsSuccess ? Ok() : BadRequest(result.Error);
    }
}

public record BiometricWebhookPayload(string DeviceId, string BiometricId, DateTime Timestamp);
