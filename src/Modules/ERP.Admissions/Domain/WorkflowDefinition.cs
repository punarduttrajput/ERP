using ERP.Shared.Domain;

namespace ERP.Admissions.Domain;

public class WorkflowDefinition : TenantEntity
{
    public string Name { get; set; } = string.Empty;
    public int Version { get; set; } = 1;
    public bool IsActive { get; set; } = true;
    public int OfferValidityDays { get; set; } = 7;
}
