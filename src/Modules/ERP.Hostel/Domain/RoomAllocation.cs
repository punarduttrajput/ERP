using ERP.Shared.Domain;

namespace ERP.Hostel.Domain;

public class RoomAllocation : TenantEntity
{
    public Guid RoomId { get; set; }
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public int AcademicYear { get; set; }
    public DateTime AllocatedAt { get; set; }
    public DateTime? VacatedAt { get; set; }
    public AllocationStatus Status { get; set; }

    public HostelRoom? Room { get; set; }
}
