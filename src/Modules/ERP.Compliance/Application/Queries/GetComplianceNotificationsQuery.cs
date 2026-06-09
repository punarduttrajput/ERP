using ERP.Compliance.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Compliance.Application.Queries;

public record GetComplianceNotificationsQuery(Guid TenantId, Guid UserId, bool UnreadOnly = false) : IRequest<Result<IReadOnlyList<ComplianceNotificationDto>>>;

public record ComplianceNotificationDto(
    Guid Id,
    Guid ComplianceItemId,
    string Message,
    DateTime SentAt,
    bool IsRead,
    string NotificationType);

public class GetComplianceNotificationsHandler : IRequestHandler<GetComplianceNotificationsQuery, Result<IReadOnlyList<ComplianceNotificationDto>>>
{
    private readonly IComplianceDbContext _db;

    public GetComplianceNotificationsHandler(IComplianceDbContext db)
    {
        _db = db;
    }

    public async Task<Result<IReadOnlyList<ComplianceNotificationDto>>> Handle(GetComplianceNotificationsQuery request, CancellationToken cancellationToken)
    {
        var query = _db.ComplianceNotifications
            .Where(x => x.TenantId == request.TenantId
                        && x.RecipientUserId == request.UserId
                        && !x.IsDeleted);

        if (request.UnreadOnly)
            query = query.Where(x => !x.IsRead);

        var items = await query
            .OrderByDescending(x => x.SentAt)
            .Select(x => new ComplianceNotificationDto(
                x.Id, x.ComplianceItemId, x.Message, x.SentAt, x.IsRead, x.NotificationType))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<ComplianceNotificationDto>>(items);
    }
}
