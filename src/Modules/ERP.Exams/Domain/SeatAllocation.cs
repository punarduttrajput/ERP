using ERP.Shared.Domain;

namespace ERP.Exams.Domain;

public class SeatAllocation : TenantEntity
{
    public Guid ExamScheduleId { get; set; }
    public Guid StudentId { get; set; }
    public string RollNumber { get; set; } = string.Empty;
    public string SeatNumber { get; set; } = string.Empty;
    public bool HallTicketGenerated { get; set; } = false;
    public bool IsEligible { get; set; } = true;
    public string? IneligibilityReason { get; set; }

    public ExamSchedule? ExamSchedule { get; set; }
}
