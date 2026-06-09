using ERP.Shared.Domain;

namespace ERP.NAAC.Domain;

public class AqarReport : TenantEntity
{
    public int AcademicYear { get; set; }
    public string Title { get; set; } = string.Empty;
    public AqarStatus Status { get; set; } = AqarStatus.Draft;
    public DateTime? SubmittedAt { get; set; }

    public ICollection<AqarSection> Sections { get; set; } = new List<AqarSection>();
}
