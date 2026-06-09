using ERP.Shared.Application.Abstractions;

namespace ERP.Shared.Infrastructure;

/// <summary>
/// Stores tenant context in AsyncLocal so every instance — including ones captured
/// in EF Core's cached model filter expressions — always reads the current request's
/// tenant rather than the one that was active when the model was first compiled.
/// </summary>
public sealed class CurrentTenant : ICurrentTenant
{
    private static readonly AsyncLocal<Guid?> _tenantId   = new();
    private static readonly AsyncLocal<string?> _tenantSlug = new();

    public Guid?   TenantId   => _tenantId.Value;
    public string? TenantSlug => _tenantSlug.Value;
    public bool    IsResolved => _tenantId.Value.HasValue;

    public void SetTenant(Guid tenantId, string tenantSlug)
    {
        _tenantId.Value   = tenantId;
        _tenantSlug.Value = tenantSlug;
    }
}
