using ERP.Shared.Domain;

namespace ERP.Exams.Domain;

public class GradingScheme : TenantEntity
{
    public string Name { get; set; } = string.Empty;
    public bool IsDefault { get; set; }

    public ICollection<GradeRule> GradeRules { get; set; } = new List<GradeRule>();
}
