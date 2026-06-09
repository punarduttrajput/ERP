using ERP.Accreditation.Infrastructure;
using ERP.Fees.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ERP.Chatbot.Application.Services.IntentHandlers;

public sealed class KpiQueryIntentHandler
{
    private readonly IAccreditationDbContext _accreditationDb;
    private readonly IFeesDbContext _feesDb;

    public KpiQueryIntentHandler(IAccreditationDbContext accreditationDb, IFeesDbContext feesDb)
    {
        _accreditationDb = accreditationDb;
        _feesDb = feesDb;
    }

    public async Task<string> HandleAsync(Guid tenantId, Guid userId, string userMessage, CancellationToken ct)
    {
        var msg = userMessage.ToLowerInvariant();

        var enrollmentSummary = await _accreditationDb.EvidenceSummaries
            .Where(e => e.Category == "StudentEnrollment")
            .OrderByDescending(e => e.ComputedAt)
            .FirstOrDefaultAsync(ct);

        var feesDueCount = await _feesDb.StudentFeeAccounts
            .CountAsync(a => a.DueAmount > 0, ct);

        var enrolledCount = (int)(enrollmentSummary?.NumericValue ?? 0);

        return $"There are currently {enrolledCount} students enrolled. {feesDueCount} student(s) have outstanding fee dues.";
    }
}
