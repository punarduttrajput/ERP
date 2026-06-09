using MediatR;

namespace ERP.RBAC.Application.Events;

public record UserRoleChangedEvent(Guid TenantId, Guid UserId) : INotification;
