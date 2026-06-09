using ERP.Shared.Domain;

namespace ERP.LMS.Domain;

public class Assignment : TenantEntity
{
    public Guid SubjectId { get; set; }
    public Guid BatchId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public decimal MaxMarks { get; set; }
    public bool IsVisible { get; set; } = true;
    public Guid AssignmentCreatedBy { get; set; }
    public ICollection<AssignmentSubmission> Submissions { get; set; } = new List<AssignmentSubmission>();
}
