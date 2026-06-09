using ERP.Shared.Domain;

namespace ERP.OBE.Domain;

public class CoPsoMapping : TenantEntity
{
    public Guid SubjectId { get; set; }
    public string CourseOutcomeCode { get; set; } = string.Empty;
    public string PsoCode { get; set; } = string.Empty;
    public int CorrelationLevel { get; set; }
    public Guid ProgramId { get; set; }
}
