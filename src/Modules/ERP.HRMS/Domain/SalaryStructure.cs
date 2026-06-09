using ERP.Shared.Domain;

namespace ERP.HRMS.Domain;

public class SalaryStructure : TenantEntity
{
    public string Name { get; set; } = string.Empty;
    public DateOnly EffectiveFrom { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<SalaryComponent> Components { get; set; } = new List<SalaryComponent>();
}
