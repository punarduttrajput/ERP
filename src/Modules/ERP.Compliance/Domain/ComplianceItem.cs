using ERP.Shared.Domain;

namespace ERP.Compliance.Domain;

public class ComplianceItem : TenantEntity
{
    public ComplianceAuthority Authority { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateOnly DueDate { get; set; }
    public Guid? ResponsiblePersonId { get; set; }
    // Denormalised to avoid joins in alert jobs that run without HTTP context
    public string? ResponsiblePersonName { get; set; }
    public ComplianceStatus Status { get; set; } = ComplianceStatus.Pending;
    public DateTime? CompletedAt { get; set; }
    public Guid? CompletedBy { get; set; }
    public string? SubmissionReference { get; set; }
    public string? Notes { get; set; }
    public int AcademicYear { get; set; }
    public bool IsRecurring { get; set; } = false;
    public string? RecurrencePattern { get; set; }
}
