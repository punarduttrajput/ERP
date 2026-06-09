using ERP.Shared.Domain;

namespace ERP.Academics.Domain;

public class CourseOutcome : TenantEntity
{
    public Guid SubjectId { get; set; }
    public string Code { get; set; } = default!;
    public string Description { get; set; } = default!;

    public Subject? Subject { get; set; }
}
