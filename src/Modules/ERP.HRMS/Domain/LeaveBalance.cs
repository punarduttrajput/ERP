using ERP.Shared.Domain;

namespace ERP.HRMS.Domain;

public class LeaveBalance : TenantEntity
{
    public Guid EmployeeId { get; set; }
    public Guid LeaveTypeId { get; set; }
    public int Year { get; set; }
    public decimal TotalDays { get; set; }
    public decimal UsedDays { get; set; } = 0;
    public decimal PendingDays { get; set; } = 0;
    public decimal AvailableDays => TotalDays - UsedDays - PendingDays;
}
