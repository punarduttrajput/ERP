using ERP.Shared.Domain;

namespace ERP.Alumni.Domain;

public class AlumniEvent : TenantEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public EventType EventType { get; set; }
    public DateOnly EventDate { get; set; }
    public TimeOnly? EventTime { get; set; }
    public string? VenueOrLink { get; set; }
    public int? MaxParticipants { get; set; }
    public int RegisteredCount { get; set; } = 0;
    public bool IsPublished { get; set; } = false;
    public Guid OrganisedBy { get; set; }
    public ICollection<EventRegistration> Registrations { get; set; } = new List<EventRegistration>();
}
