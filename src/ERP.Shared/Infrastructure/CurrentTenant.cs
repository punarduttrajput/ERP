using ERP.Shared.Application.Abstractions;

namespace ERP.Shared.Infrastructure;

public sealed class CurrentTenant : ICurrentTenant
{
    private Guid? _tenantId;
    private string? _tenantSlug;

    public Guid? TenantId => _tenantId;
    public string? TenantSlug => _tenantSlug;
    public bool IsResolved => _tenantId.HasValue;

    public void SetTenant(Guid tenantId, string tenantSlug)
    {
        _tenantId = tenantId;
        _tenantSlug = tenantSlug;
    }
}
