using ERP.Shared.Domain;

namespace ERP.Exams.Domain;

public class ArrearRegistration : TenantEntity
{
    public Guid StudentId { get; set; }
    public Guid SubjectId { get; set; }
    public Guid SemesterId { get; set; }
    public Guid ExamSemesterId { get; set; }
    public DateTime RegisteredAt { get; set; }
    public bool IsApproved { get; set; } = false;
}
