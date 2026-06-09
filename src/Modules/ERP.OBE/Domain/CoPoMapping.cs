using ERP.Shared.Domain;

namespace ERP.OBE.Domain;

public class CoPoMapping : TenantEntity
{
    public Guid SubjectId { get; set; }
    public string CourseOutcomeCode { get; set; } = string.Empty;
    public string ProgramOutcomeCode { get; set; } = string.Empty;
    public int CorrelationLevel { get; set; }
    public Guid ProgramId { get; set; }
}
