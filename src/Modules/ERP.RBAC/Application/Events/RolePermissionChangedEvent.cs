using MediatR;

namespace ERP.RBAC.Application.Events;

public record RolePermissionChangedEvent(Guid TenantId, Guid RoleId) : INotification;
