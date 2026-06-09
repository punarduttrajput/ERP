using ERP.Shared.Domain;

namespace ERP.Exams.Domain;

public class GradeRule : TenantEntity
{
    public Guid GradingSchemeId { get; set; }
    public decimal MinMarks { get; set; }
    public decimal MaxMarks { get; set; }
    public string GradeLetter { get; set; } = string.Empty;
    public decimal GradePoints { get; set; }

    public GradingScheme? GradingScheme { get; set; }
}
