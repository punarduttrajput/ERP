using ERP.Shared.Domain;

namespace ERP.HRMS.Domain;

public class SalaryComponent : TenantEntity
{
    public Guid SalaryStructureId { get; set; }
    public string Name { get; set; } = string.Empty;
    public ComponentType ComponentType { get; set; }
    public bool IsPercentage { get; set; }
    public decimal? Amount { get; set; }
    public decimal? Percentage { get; set; }
    public string? BaseComponent { get; set; }
    public bool IsStatutory { get; set; }

    public SalaryStructure? SalaryStructure { get; set; }
}
