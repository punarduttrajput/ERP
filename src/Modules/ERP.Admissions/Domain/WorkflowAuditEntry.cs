using ERP.Shared.Domain;

namespace ERP.Admissions.Domain;

public class WorkflowAuditEntry : TenantEntity
{
    public Guid ApplicationId { get; set; }
    public ApplicationState FromState { get; set; }
    public ApplicationState ToState { get; set; }
    public Guid ActorId { get; set; }
    public string? Reason { get; set; }

    public AdmissionApplication? Application { get; set; }
}
