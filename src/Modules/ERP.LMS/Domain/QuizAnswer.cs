using ERP.Shared.Domain;

namespace ERP.LMS.Domain;

public class QuizAnswer : TenantEntity
{
    public Guid AttemptId { get; set; }
    public Guid QuestionId { get; set; }
    public string? AnswerText { get; set; }
    // null for ShortAnswer — requires manual grading
    public bool? IsCorrect { get; set; }
    public decimal? MarksAwarded { get; set; }
    public QuizAttempt? Attempt { get; set; }
}
