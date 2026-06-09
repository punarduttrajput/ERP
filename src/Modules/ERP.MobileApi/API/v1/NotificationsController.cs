using ERP.MobileApi.Application.Commands;
using ERP.MobileApi.Application.Queries;
using ERP.MobileApi.Domain;
using ERP.MobileApi.Infrastructure;
using ERP.Shared.Application.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.MobileApi.API.v1;

[ApiController]
[Route("api/mobile/v1/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentTenant _currentTenant;
    private readonly ICurrentUser _currentUser;
    private readonly IMobileDbContext _db;

    public NotificationsController(
        IMediator mediator,
        ICurrentTenant currentTenant,
        ICurrentUser currentUser,
        IMobileDbContext db)
    {
        _mediator = mediator;
        _currentTenant = currentTenant;
        _currentUser = currentUser;
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetNotifications(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var tenantId = _currentTenant.TenantId ?? Guid.Empty;
        var userId = _currentUser.UserId ?? Guid.Empty;
        var result = await _mediator.Send(new GetNotificationsQuery(tenantId, userId, page, pageSize), ct);
        return Ok(result);
    }

    [HttpPatch("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken ct)
    {
        var tenantId = _currentTenant.TenantId ?? Guid.Empty;
        var userId = _currentUser.UserId ?? Guid.Empty;

        var notification = await _db.PushNotifications
            .FirstOrDefaultAsync(n => n.Id == id
                && n.TenantId == tenantId
                && n.RecipientUserId == userId, ct);

        if (notification is null)
            return NotFound();

        notification.Status = NotificationStatus.Read;
        notification.ReadAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("send")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> Send([FromBody] SendNotificationRequest request, CancellationToken ct)
    {
        var tenantId = _currentTenant.TenantId ?? Guid.Empty;
        var result = await _mediator.Send(
            new SendPushNotificationCommand(
                tenantId,
                request.RecipientUserIds,
                request.Title,
                request.Body,
                request.Data,
                request.NotificationType), ct);
        return result.IsSuccess ? Ok(new { Sent = result.Value }) : BadRequest(result.Error);
    }

    public record SendNotificationRequest(
        IReadOnlyList<Guid> RecipientUserIds,
        string Title,
        string Body,
        IDictionary<string, string>? Data,
        string NotificationType
    );
}
