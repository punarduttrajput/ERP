using ERP.Shared.Domain;

namespace ERP.Finance.Domain;

public class JournalLine : TenantEntity
{
    public Guid JournalEntryId { get; set; }
    public Guid AccountId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public decimal Debit { get; set; } = 0m;
    public decimal Credit { get; set; } = 0m;
    public string? Narration { get; set; }

    public JournalEntry? Entry { get; set; }
}
