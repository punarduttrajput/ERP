using ERP.Compliance.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Compliance.Application.Commands;

public record MarkNotificationReadCommand(Guid TenantId, Guid NotificationId, Guid UserId) : IRequest<Result>;

public class MarkNotificationReadHandler : IRequestHandler<MarkNotificationReadCommand, Result>
{
    private readonly IComplianceDbContext _db;

    public MarkNotificationReadHandler(IComplianceDbContext db)
    {
        _db = db;
    }

    public async Task<Result> Handle(MarkNotificationReadCommand request, CancellationToken cancellationToken)
    {
        var notification = await _db.ComplianceNotifications
            .FirstOrDefaultAsync(x => x.Id == request.NotificationId
                                      && x.TenantId == request.TenantId
                                      && x.RecipientUserId == request.UserId
                                      && !x.IsDeleted,
                cancellationToken);

        if (notification is null)
            return Result.Failure("Notification not found.");

        notification.IsRead = true;
        notification.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
