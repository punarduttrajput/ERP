using ERP.Shared.Domain;

namespace ERP.NAAC.Domain;

public class SsrReport : TenantEntity
{
    public int AcademicYear { get; set; }
    public string Title { get; set; } = string.Empty;
    public SsrStatus Status { get; set; } = SsrStatus.Draft;
    public DateTime? SubmittedAt { get; set; }
    public Guid? ApprovedBy { get; set; }

    public ICollection<SsrSection> Sections { get; set; } = new List<SsrSection>();
}
