namespace ERP.Shared.Application.Abstractions;

public interface ICurrentTenant
{
    Guid? TenantId { get; }
    string? TenantSlug { get; }
    bool IsResolved { get; }
    void SetTenant(Guid tenantId, string tenantSlug);
}
