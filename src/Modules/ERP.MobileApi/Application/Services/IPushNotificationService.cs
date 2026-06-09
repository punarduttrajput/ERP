namespace ERP.MobileApi.Application.Services;

public interface IPushNotificationService
{
    Task<bool> SendToUserAsync(
        Guid tenantId,
        Guid userId,
        string title,
        string body,
        IDictionary<string, string>? data = null,
        CancellationToken cancellationToken = default);

    Task<int> SendToUsersAsync(
        Guid tenantId,
        IReadOnlyList<Guid> userIds,
        string title,
        string body,
        IDictionary<string, string>? data = null,
        CancellationToken cancellationToken = default);
}
