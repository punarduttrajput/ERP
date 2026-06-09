using ERP.Finance.Domain;
using ERP.Finance.Infrastructure;
using ERP.Shared.Application.Contracts;
using Microsoft.EntityFrameworkCore;

namespace ERP.Finance.Application.Services;

public class FeeCollectionEvidenceProvider : IEvidenceProvider
{
    private readonly IFinanceDbContext _db;

    public FeeCollectionEvidenceProvider(IFinanceDbContext db)
    {
        _db = db;
    }

    public string ModuleName => "Finance";

    public async Task<IReadOnlyList<EvidenceItem>> GetEvidenceAsync(
        Guid tenantId,
        int academicYear,
        CancellationToken cancellationToken = default)
    {
        var incomeLines = await _db.JournalLines
            .Where(l => l.TenantId == tenantId
                     && !l.IsDeleted
                     && l.Entry != null
                     && l.Entry.Status == EntryStatus.Posted
                     && !l.Entry.IsDeleted
                     && l.Entry.EntryDate.Year == academicYear
                     && l.Credit > 0)
            .GroupBy(l => l.AccountId)
            .Select(g => new
            {
                AccountId = g.Key,
                AccountCode = g.First().AccountCode,
                AccountName = g.First().AccountName,
                TotalCredit = g.Sum(l => l.Credit)
            })
            .ToListAsync(cancellationToken);

        return incomeLines.Select(l => new EvidenceItem(
            Module: "Finance",
            Category: "FeeCollection",
            Key: l.AccountId.ToString(),
            Label: $"{l.AccountName} ({l.AccountCode})",
            NumericValue: l.TotalCredit,
            TextValue: null,
            RecordedAt: DateTime.UtcNow,
            Metadata: new Dictionary<string, string>
            {
                { "accountCode", l.AccountCode },
                { "academicYear", academicYear.ToString() }
            }
        )).ToList();
    }
}
