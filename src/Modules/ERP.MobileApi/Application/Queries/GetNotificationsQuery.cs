using ERP.MobileApi.Domain;
using ERP.MobileApi.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.MobileApi.Application.Queries;

public record GetNotificationsQuery(Guid TenantId, Guid UserId, int Page, int PageSize)
    : IRequest<PagedResult<NotificationDto>>;

public class GetNotificationsHandler : IRequestHandler<GetNotificationsQuery, PagedResult<NotificationDto>>
{
    private readonly IMobileDbContext _db;

    public GetNotificationsHandler(IMobileDbContext db) => _db = db;

    public async Task<PagedResult<NotificationDto>> Handle(GetNotificationsQuery request, CancellationToken cancellationToken)
    {
        var query = _db.PushNotifications
            .Where(n => n.TenantId == request.TenantId
                && n.RecipientUserId == request.UserId
                && !n.IsDeleted);

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(n => n.SentAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(n => new NotificationDto(
                n.Id, n.Title, n.Body,
                n.SentAt ?? n.CreatedAt,
                n.Status == NotificationStatus.Read))
            .ToListAsync(cancellationToken);

        return new PagedResult<NotificationDto>(items, total, request.Page, request.PageSize);
    }
}
