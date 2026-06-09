using ERP.Finance.Domain;
using ERP.Finance.Infrastructure;
using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Finance.Application.Commands;

public record PostJournalEntryCommand(Guid TenantId, Guid EntryId, Guid? PostedBy) : IRequest<Result>;

public class PostJournalEntryHandler : IRequestHandler<PostJournalEntryCommand, Result>
{
    private readonly IFinanceDbContext _db;

    public PostJournalEntryHandler(IFinanceDbContext db)
    {
        _db = db;
    }

    public async Task<Result> Handle(PostJournalEntryCommand request, CancellationToken cancellationToken)
    {
        var entry = await _db.JournalEntries
            .Include(e => e.Lines)
            .FirstOrDefaultAsync(e => e.Id == request.EntryId && e.TenantId == request.TenantId, cancellationToken);

        if (entry is null)
            return Result.Failure("Journal entry not found.");

        if (entry.Status != EntryStatus.Draft)
            return Result.Failure($"Journal entry is not in Draft status (current: {entry.Status}).");

        var accountIds = entry.Lines.Select(l => l.AccountId).Distinct().ToList();
        var accounts = await _db.GlAccounts
            .Where(a => accountIds.Contains(a.Id) && a.TenantId == request.TenantId)
            .ToListAsync(cancellationToken);

        var accountMap = accounts.ToDictionary(a => a.Id);

        foreach (var line in entry.Lines)
        {
            if (!accountMap.TryGetValue(line.AccountId, out var account))
                continue;

            // Credits increase the balance, debits decrease — the running balance is sign-aware.
            // Reports then interpret the sign by AccountType (e.g. Asset with positive balance is debit-normal).
            account.Balance += line.Credit - line.Debit;
        }

        // Update budget spent amounts for expense account lines
        var expenseAccountIds = accountMap.Values
            .Where(a => a.AccountType == AccountType.Expense)
            .Select(a => a.Id)
            .ToHashSet();

        var expenseLines = entry.Lines.Where(l => expenseAccountIds.Contains(l.AccountId)).ToList();
        if (expenseLines.Any())
        {
            var budgetLines = await _db.BudgetLines
                .Where(bl => bl.TenantId == request.TenantId && expenseAccountIds.Contains(bl.AccountId))
                .ToListAsync(cancellationToken);

            foreach (var expenseLine in expenseLines)
            {
                var matchingBudgetLines = budgetLines.Where(bl => bl.AccountId == expenseLine.AccountId);
                foreach (var bl in matchingBudgetLines)
                {
                    bl.SpentAmount += expenseLine.Debit;
                }
            }

            // Roll up to parent Budget.TotalSpent
            var budgetIds = budgetLines.Select(bl => bl.BudgetId).Distinct().ToList();
            var budgets = await _db.Budgets
                .Where(b => budgetIds.Contains(b.Id))
                .ToListAsync(cancellationToken);

            foreach (var budget in budgets)
            {
                var lines = budgetLines.Where(bl => bl.BudgetId == budget.Id);
                budget.TotalSpent = lines.Sum(bl => bl.SpentAmount);
            }
        }

        entry.Status = EntryStatus.Posted;
        entry.PostedAt = DateTime.UtcNow;
        entry.PostedBy = request.PostedBy;

        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
