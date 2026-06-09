using ERP.Shared.Domain;

namespace ERP.Exams.Domain;

public class ExternalMark : TenantEntity
{
    public Guid ExamScheduleId { get; set; }
    public Guid StudentId { get; set; }
    public decimal Marks { get; set; }
    public decimal MaxMarks { get; set; } = 100;
    public bool IsAbsent { get; set; } = false;
    public Guid EnteredBy { get; set; }
}
