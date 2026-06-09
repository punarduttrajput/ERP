using ERP.Shared.Domain;

namespace ERP.Research.Domain;

public class Publication : TenantEntity
{
    public Guid FacultyId { get; set; }
    public string FacultyName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public PublicationType PublicationType { get; set; }
    public string VenueName { get; set; } = string.Empty;
    public string? Isbn { get; set; }
    public string? IssueVolume { get; set; }
    public string? PageNumbers { get; set; }
    public int PublicationYear { get; set; }
    public string? Doi { get; set; }
    public decimal? ImpactFactor { get; set; }
    public PublicationIndex Index { get; set; }
    public bool IsUgcListed { get; set; }
    public Guid? ResearchProjectId { get; set; }
}
