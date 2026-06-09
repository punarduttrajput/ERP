using ERP.Shared.Domain;

namespace ERP.Exams.Domain;

public class InternalMark : TenantEntity
{
    public Guid SubjectId { get; set; }
    public Guid StudentId { get; set; }
    public Guid SemesterId { get; set; }
    public decimal Marks { get; set; }
    public decimal MaxMarks { get; set; } = 50;
    public Guid EnteredBy { get; set; }
}
