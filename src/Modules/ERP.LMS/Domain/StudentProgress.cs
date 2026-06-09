using ERP.Shared.Domain;

namespace ERP.LMS.Domain;

public class StudentProgress : TenantEntity
{
    public Guid StudentId { get; set; }
    public Guid SubjectId { get; set; }
    public Guid BatchId { get; set; }
    public int ContentViewedCount { get; set; } = 0;
    public int TotalContentCount { get; set; } = 0;
    public int AssignmentsSubmitted { get; set; } = 0;
    public int TotalAssignments { get; set; } = 0;
    public int QuizzesTaken { get; set; } = 0;
    public int TotalQuizzes { get; set; } = 0;
    public decimal AverageQuizScore { get; set; } = 0;
    public DateTime? LastActivityAt { get; set; }
}
