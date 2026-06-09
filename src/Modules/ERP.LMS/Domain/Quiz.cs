using ERP.Shared.Domain;

namespace ERP.LMS.Domain;

public class Quiz : TenantEntity
{
    public Guid SubjectId { get; set; }
    public Guid BatchId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Instructions { get; set; }
    public int DurationMinutes { get; set; }
    public int MaxAttempts { get; set; } = 1;
    public bool IsVisible { get; set; } = true;
    public Guid QuizCreatedBy { get; set; }
    public ICollection<QuizQuestion> Questions { get; set; } = new List<QuizQuestion>();
}
