using ERP.Shared.Domain;

namespace ERP.Exams.Domain;

public class ExamSchedule : TenantEntity
{
    public Guid SemesterId { get; set; }
    public Guid SubjectId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public DateOnly ExamDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public string Venue { get; set; } = string.Empty;
    public int MaxMarks { get; set; } = 100;
    public int PassingMarks { get; set; } = 40;

    public ICollection<SeatAllocation> SeatAllocations { get; set; } = new List<SeatAllocation>();
}
