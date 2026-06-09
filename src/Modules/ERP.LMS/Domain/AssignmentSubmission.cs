using ERP.Shared.Domain;

namespace ERP.LMS.Domain;

public class AssignmentSubmission : TenantEntity
{
    public Guid AssignmentId { get; set; }
    public Guid StudentId { get; set; }
    public string BlobUrl { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
    public SubmissionStatus Status { get; set; }
    public decimal? MarksAwarded { get; set; }
    public string? FacultyFeedback { get; set; }
    public Guid? GradedBy { get; set; }
    public DateTime? GradedAt { get; set; }
    public Assignment? Assignment { get; set; }
}
