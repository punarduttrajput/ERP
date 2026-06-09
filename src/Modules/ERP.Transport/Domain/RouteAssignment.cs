using ERP.Shared.Domain;

namespace ERP.Transport.Domain;

public class RouteAssignment : TenantEntity
{
    public Guid RouteId { get; set; }
    public Guid StopId { get; set; }
    public Guid MemberId { get; set; }
    public string MemberType { get; set; } = string.Empty;
    public string MemberName { get; set; } = string.Empty;
    public int AcademicYear { get; set; }
    public bool IsActive { get; set; } = true;
}
