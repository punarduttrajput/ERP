using ERP.Shared.Domain;

namespace ERP.OBE.Domain;

public class ActionPlan : TenantEntity
{
    public Guid GapId { get; set; }
    public Guid SubjectId { get; set; }
    public string CourseOutcomeCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid? AssignedTo { get; set; }
    public DateOnly? TargetDate { get; set; }
    public ActionPlanStatus Status { get; set; }
    public string? Outcome { get; set; }
}
