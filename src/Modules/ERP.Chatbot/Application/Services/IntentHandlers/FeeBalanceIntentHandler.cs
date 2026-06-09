using ERP.Fees.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ERP.Chatbot.Application.Services.IntentHandlers;

public sealed class FeeBalanceIntentHandler
{
    private readonly IFeesDbContext _feesDb;

    public FeeBalanceIntentHandler(IFeesDbContext feesDb) => _feesDb = feesDb;

    public async Task<string> HandleAsync(Guid tenantId, Guid userId, string userMessage, CancellationToken ct)
    {
        var account = await _feesDb.StudentFeeAccounts
            .FirstOrDefaultAsync(a => a.StudentId == userId && !a.IsFullyPaid, ct);

        if (account is null)
            return "Your fee account is fully paid. No outstanding dues.";

        var nextInstallment = await _feesDb.FeeInstallments
            .Where(i => i.AccountId == account.Id && !i.IsPaid)
            .OrderBy(i => i.DueDate)
            .FirstOrDefaultAsync(ct);

        if (nextInstallment is not null)
            return $"Your current outstanding fee balance is ₹{account.DueAmount:N2}. The next installment of ₹{nextInstallment.TotalDue:N2} is due on {nextInstallment.DueDate:dd MMM yyyy}.";

        return $"Your current outstanding fee balance is ₹{account.DueAmount:N2}.";
    }
}
