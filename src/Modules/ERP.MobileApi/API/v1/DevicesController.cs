using ERP.MobileApi.Application.Commands;
using ERP.MobileApi.Domain;
using ERP.Shared.Application.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.MobileApi.API.v1;

[ApiController]
[Route("api/mobile/v1/devices")]
[Authorize]
public class DevicesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentTenant _currentTenant;
    private readonly ICurrentUser _currentUser;

    public DevicesController(IMediator mediator, ICurrentTenant currentTenant, ICurrentUser currentUser)
    {
        _mediator = mediator;
        _currentTenant = currentTenant;
        _currentUser = currentUser;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDeviceRequest request, CancellationToken ct)
    {
        var tenantId = _currentTenant.TenantId ?? Guid.Empty;
        var userId = _currentUser.UserId ?? Guid.Empty;
        var result = await _mediator.Send(
            new RegisterDeviceCommand(tenantId, userId, request.DeviceToken, request.Platform, request.AppVersion), ct);
        return result.IsSuccess ? Ok(new { Id = result.Value }) : BadRequest(result.Error);
    }

    [HttpDelete("unregister")]
    public async Task<IActionResult> Unregister([FromBody] UnregisterDeviceRequest request, CancellationToken ct)
    {
        var tenantId = _currentTenant.TenantId ?? Guid.Empty;
        var userId = _currentUser.UserId ?? Guid.Empty;
        var result = await _mediator.Send(new UnregisterDeviceCommand(tenantId, userId, request.DeviceToken), ct);
        return result.IsSuccess ? NoContent() : NotFound(result.Error);
    }

    public record RegisterDeviceRequest(string DeviceToken, DevicePlatform Platform, string? AppVersion);
    public record UnregisterDeviceRequest(string DeviceToken);
}
