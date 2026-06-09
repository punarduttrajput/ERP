using ERP.Shared.Domain;

namespace ERP.Timetable.Domain;

public class SubstituteAssignment : TenantEntity
{
    public Guid OriginalEntryId { get; set; }
    public Guid SubstituteFacultyUserId { get; set; }
    public DateOnly Date { get; set; }
    public string? Reason { get; set; }
}
