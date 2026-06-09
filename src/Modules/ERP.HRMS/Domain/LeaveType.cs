using ERP.Shared.Domain;

namespace ERP.HRMS.Domain;

public class LeaveType : TenantEntity
{
    public string Name { get; set; } = string.Empty;
    public int DaysAllowedPerYear { get; set; }
    public bool IsCarryForward { get; set; }
    public int? MaxCarryForwardDays { get; set; }
}
