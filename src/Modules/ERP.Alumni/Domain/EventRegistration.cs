using ERP.Shared.Domain;

namespace ERP.Alumni.Domain;

public class EventRegistration : TenantEntity
{
    public Guid EventId { get; set; }
    public Guid AlumniId { get; set; }
    public DateTime RegisteredAt { get; set; }
    public DateTime? AttendedAt { get; set; }
    public AlumniEvent? Event { get; set; }
}
