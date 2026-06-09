namespace ERP.Shared.Application.Abstractions;

public interface ICurrentUser
{
    Guid? UserId { get; }
    string? Email { get; }
    string? Name { get; }
    Guid? TenantId { get; }
    IReadOnlyList<string> Roles { get; }
    bool IsAuthenticated { get; }
    bool HasPermission(string permission);
}
