namespace ERP.Shared.Domain;

public abstract class TenantEntity : BaseEntity
{
    public Guid TenantId { get; set; }
}
