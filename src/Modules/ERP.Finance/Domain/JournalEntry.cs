using ERP.Shared.Domain;

namespace ERP.Finance.Domain;

public class JournalEntry : TenantEntity
{
    public string EntryNumber { get; set; } = string.Empty;
    public DateOnly EntryDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Reference { get; set; }
    public EntryStatus Status { get; set; } = EntryStatus.Draft;
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public DateTime? PostedAt { get; set; }
    public Guid? PostedBy { get; set; }
    public Guid? ReversedByEntryId { get; set; }

    public ICollection<JournalLine> Lines { get; set; } = new List<JournalLine>();
}
