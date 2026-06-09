using ERP.Shared.Domain;

namespace ERP.SIS.Domain;

public class StudentFamily : TenantEntity
{
    public Guid StudentId { get; set; }
    public string Relation { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Occupation { get; set; }
    public string? MobileNumber { get; set; }
    public string? Email { get; set; }
    public decimal? AnnualIncome { get; set; }

    public Student? Student { get; set; }
}
