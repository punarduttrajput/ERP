using ERP.Shared.Domain;

namespace ERP.Fees.Domain;

public class StudentScholarship : TenantEntity
{
    public Guid StudentId { get; set; }
    public Guid ScholarshipId { get; set; }
    public int AcademicYear { get; set; }
    public decimal DiscountApplied { get; set; }
    public Guid? AppliedBy { get; set; }
    public DateTime AppliedAt { get; set; }
    public Scholarship? Scholarship { get; set; }
}
