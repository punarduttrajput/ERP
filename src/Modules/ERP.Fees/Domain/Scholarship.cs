using ERP.Shared.Domain;

namespace ERP.Fees.Domain;

public class Scholarship : TenantEntity
{
    public string Name { get; set; } = string.Empty;
    public ScholarshipType ScholarshipType { get; set; }
    public decimal? DiscountAmount { get; set; }
    public decimal? DiscountPercent { get; set; }
    public decimal? MinMeritScore { get; set; }
    public string? EligibleCategories { get; set; }
    public bool IsActive { get; set; } = true;
    public int? MaxBeneficiaries { get; set; }
}
