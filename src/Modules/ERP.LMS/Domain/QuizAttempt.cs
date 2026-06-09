using ERP.Shared.Domain;

namespace ERP.LMS.Domain;

public class QuizAttempt : TenantEntity
{
    public Guid QuizId { get; set; }
    public Guid StudentId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public decimal? TotalMarks { get; set; }
    public bool IsCompleted { get; set; } = false;
    public ICollection<QuizAnswer> Answers { get; set; } = new List<QuizAnswer>();
}
