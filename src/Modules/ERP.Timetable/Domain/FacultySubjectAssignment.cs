using ERP.Shared.Domain;

namespace ERP.Timetable.Domain;

public class FacultySubjectAssignment : TenantEntity
{
    public Guid FacultyUserId { get; set; }
    public Guid SubjectId { get; set; }
    public Guid SemesterId { get; set; }
    public Guid BatchId { get; set; }
    public int HoursPerWeek { get; set; }
}
