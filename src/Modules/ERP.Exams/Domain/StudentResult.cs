using ERP.Shared.Domain;

namespace ERP.Exams.Domain;

public class StudentResult : TenantEntity
{
    public Guid StudentId { get; set; }
    public Guid SemesterId { get; set; }
    public Guid SubjectId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public decimal InternalMarks { get; set; }
    public decimal ExternalMarks { get; set; }
    public decimal TotalMarks { get; set; }
    public decimal MaxMarks { get; set; }
    public string GradeLetter { get; set; } = string.Empty;
    public decimal GradePoints { get; set; }
    public int Credits { get; set; }
    public ResultStatus Status { get; set; }
    public bool IsPublished { get; set; } = false;
    public DateTime? PublishedAt { get; set; }
    public decimal? GPA { get; set; }
    public decimal? CGPA { get; set; }
}
