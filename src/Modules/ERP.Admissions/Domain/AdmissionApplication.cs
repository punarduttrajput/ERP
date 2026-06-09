using ERP.Shared.Domain;

namespace ERP.Admissions.Domain;

public class AdmissionApplication : TenantEntity
{
    public string ApplicantName { get; set; } = string.Empty;
    public string ApplicantEmail { get; set; } = string.Empty;
    public string ApplicantMobile { get; set; } = string.Empty;
    public Guid ProgramId { get; set; }
    public string ProgramName { get; set; } = string.Empty;
    public string Category { get; set; } = "General";
    public int AcademicYear { get; set; }
    public ApplicationState State { get; set; } = ApplicationState.Draft;
    public string? RejectionReason { get; set; }
    public decimal? MeritScore { get; set; }
    public int? MeritRank { get; set; }
    public DateTime? OfferExpiresAt { get; set; }
    public DateTime? EnrolledAt { get; set; }

    public Guid WorkflowDefinitionId { get; set; }
    public int WorkflowDefinitionVersion { get; set; }

    public ICollection<ApplicationDocument> Documents { get; set; } = new List<ApplicationDocument>();
    public ICollection<WorkflowAuditEntry> AuditEntries { get; set; } = new List<WorkflowAuditEntry>();

    public bool CanTransitionTo(ApplicationState target) => (State, target) switch
    {
        (ApplicationState.Draft,              ApplicationState.Submitted)         => true,
        (ApplicationState.Submitted,          ApplicationState.UnderVerification) => true,
        (ApplicationState.UnderVerification,  ApplicationState.Verified)          => true,
        (ApplicationState.UnderVerification,  ApplicationState.Rejected)          => true,
        (ApplicationState.Verified,           ApplicationState.MeritEvaluated)    => true,
        (ApplicationState.MeritEvaluated,     ApplicationState.OfferMade)         => true,
        (ApplicationState.MeritEvaluated,     ApplicationState.Rejected)          => true,
        (ApplicationState.OfferMade,          ApplicationState.OfferAccepted)     => true,
        (ApplicationState.OfferMade,          ApplicationState.Rejected)          => true,
        (ApplicationState.OfferAccepted,      ApplicationState.Enrolled)          => true,
        (ApplicationState.Draft,              ApplicationState.Withdrawn)         => true,
        (ApplicationState.Submitted,          ApplicationState.Withdrawn)         => true,
        (ApplicationState.UnderVerification,  ApplicationState.Withdrawn)         => true,
        (ApplicationState.Verified,           ApplicationState.Withdrawn)         => true,
        (ApplicationState.OfferMade,          ApplicationState.Withdrawn)         => true,
        _ => false
    };

    public void Transition(ApplicationState target, Guid actorId, string? reason = null)
    {
        if (!CanTransitionTo(target))
            throw new InvalidOperationException($"Cannot transition from {State} to {target}.");

        AuditEntries.Add(new WorkflowAuditEntry
        {
            ApplicationId = Id,
            TenantId = TenantId,
            FromState = State,
            ToState = target,
            ActorId = actorId,
            Reason = reason,
            CreatedAt = DateTime.UtcNow
        });

        State = target;
        UpdatedAt = DateTime.UtcNow;
    }
}
