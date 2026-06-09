using ERP.Shared.Domain;

namespace ERP.Academics.Domain;

public class CurriculumEntry : TenantEntity
{
    public Guid ProgramId { get; set; }
    public int SemesterNumber { get; set; }
    public Guid SubjectId { get; set; }
    public bool IsElective { get; set; } = false;

    public Subject? Subject { get; set; }
}
