using ERP.Shared.Domain;

namespace ERP.Library.Domain;

public class Fine : TenantEntity
{
    public Guid IssueId { get; set; }
    public Guid MemberId { get; set; }
    public int DaysOverdue { get; set; }
    public decimal FinePerDay { get; set; }
    public decimal TotalFine { get; set; }
    public FineStatus Status { get; set; } = FineStatus.Pending;
    public DateTime? CollectedAt { get; set; }
    public Guid? WaivedBy { get; set; }
    public string? WaivedReason { get; set; }

    public BookIssue? Issue { get; set; }
}
