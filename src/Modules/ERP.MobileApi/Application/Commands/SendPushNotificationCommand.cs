using ERP.MobileApi.Application.Services;
using ERP.MobileApi.Domain;
using ERP.MobileApi.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.MobileApi.Application.Commands;

public record SendPushNotificationCommand(
    Guid TenantId,
    IReadOnlyList<Guid> RecipientUserIds,
    string Title,
    string Body,
    IDictionary<string, string>? Data,
    string NotificationType
) : IRequest<Result<int>>;

public class SendPushNotificationHandler : IRequestHandler<SendPushNotificationCommand, Result<int>>
{
    private readonly IMobileDbContext _db;
    private readonly IPushNotificationService _pushService;

    public SendPushNotificationHandler(IMobileDbContext db, IPushNotificationService pushService)
    {
        _db = db;
        _pushService = pushService;
    }

    public async Task<Result<int>> Handle(SendPushNotificationCommand request, CancellationToken cancellationToken)
    {
        var activeDevices = await _db.DeviceRegistrations
            .Where(x => x.TenantId == request.TenantId
                && request.RecipientUserIds.Contains(x.UserId)
                && x.IsActive)
            .ToListAsync(cancellationToken);

        if (activeDevices.Count == 0)
            return Result<int>.Success(0);

        var userIds = activeDevices.Select(d => d.UserId).Distinct().ToList();
        var sent = await _pushService.SendToUsersAsync(
            request.TenantId, userIds, request.Title, request.Body, request.Data, cancellationToken);

        var dataJson = request.Data != null
            ? System.Text.Json.JsonSerializer.Serialize(request.Data)
            : null;

        var now = DateTime.UtcNow;
        foreach (var device in activeDevices)
        {
            var notification = new PushNotification
            {
                TenantId = request.TenantId,
                RecipientUserId = device.UserId,
                Title = request.Title,
                Body = request.Body,
                Data = dataJson,
                Status = NotificationStatus.Sent,
                SentAt = now,
                Platform = device.Platform,
                NotificationType = request.NotificationType
            };
            await _db.PushNotifications.AddAsync(notification, cancellationToken);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Result<int>.Success(sent);
    }
}
