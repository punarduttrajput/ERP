using ERP.Finance.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Finance.Application.Commands;

public record UpdateBudgetActualCommand(Guid TenantId, Guid AccountId, decimal Amount) : IRequest<Result>;

public class UpdateBudgetActualHandler : IRequestHandler<UpdateBudgetActualCommand, Result>
{
    private readonly IFinanceDbContext _db;

    public UpdateBudgetActualHandler(IFinanceDbContext db)
    {
        _db = db;
    }

    public async Task<Result> Handle(UpdateBudgetActualCommand request, CancellationToken cancellationToken)
    {
        var lines = await _db.BudgetLines
            .Where(bl => bl.TenantId == request.TenantId && bl.AccountId == request.AccountId)
            .ToListAsync(cancellationToken);

        foreach (var line in lines)
            line.SpentAmount += request.Amount;

        var budgetIds = lines.Select(l => l.BudgetId).Distinct().ToList();
        var budgets = await _db.Budgets
            .Where(b => budgetIds.Contains(b.Id))
            .ToListAsync(cancellationToken);

        foreach (var budget in budgets)
        {
            var relatedLines = lines.Where(l => l.BudgetId == budget.Id);
            budget.TotalSpent += relatedLines.Sum(l => request.Amount);
        }

        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
