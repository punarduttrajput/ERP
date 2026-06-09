using ERP.Shared.Domain;

namespace ERP.Research.Domain;

public class Patent : TenantEntity
{
    public string Title { get; set; } = string.Empty;
    // CSV of inventor names; a future enhancement would normalise to a junction table
    public string Inventors { get; set; } = string.Empty;
    public string? ApplicationNumber { get; set; }
    public DateOnly? FilingDate { get; set; }
    public DateOnly? GrantDate { get; set; }
    public string? GrantNumber { get; set; }
    public PatentStatus Status { get; set; }
    public string PatentOffice { get; set; } = string.Empty;
    public Guid? ResearchProjectId { get; set; }
}
