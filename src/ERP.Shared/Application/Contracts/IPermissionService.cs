namespace ERP.Shared.Application.Contracts;

public interface IPermissionService
{
    Task<UserPermissions> GetUserPermissionsAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default);
    Task InvalidateAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default);
    Task InvalidateTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

public class UserPermissions
{
    public string[] Roles { get; set; } = Array.Empty<string>();
    public string[] Permissions { get; set; } = Array.Empty<string>();
}
