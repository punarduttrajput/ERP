using ERP.Shared.Domain;

namespace ERP.Fees.Domain;

public class FeeComponent : TenantEntity
{
    public Guid FeeStructureId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public bool IsRefundable { get; set; } = false;
    public FeeStructure? FeeStructure { get; set; }
}
