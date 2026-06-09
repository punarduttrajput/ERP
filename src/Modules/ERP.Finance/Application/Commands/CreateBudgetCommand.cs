using ERP.Finance.Domain;
using ERP.Finance.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;

namespace ERP.Finance.Application.Commands;

public record BudgetLineInput(Guid AccountId, string AccountName, decimal AllocatedAmount);

public record CreateBudgetCommand(
    Guid TenantId,
    Guid DepartmentId,
    string DepartmentName,
    int AcademicYear,
    IReadOnlyList<BudgetLineInput> Lines
) : IRequest<Result<Guid>>;

public class CreateBudgetHandler : IRequestHandler<CreateBudgetCommand, Result<Guid>>
{
    private readonly IFinanceDbContext _db;

    public CreateBudgetHandler(IFinanceDbContext db)
    {
        _db = db;
    }

    public async Task<Result<Guid>> Handle(CreateBudgetCommand request, CancellationToken cancellationToken)
    {
        var exists = _db.Budgets.Any(b =>
            b.TenantId == request.TenantId &&
            b.DepartmentId == request.DepartmentId &&
            b.AcademicYear == request.AcademicYear);

        if (exists)
            return Result.Failure<Guid>("A budget for this department and academic year already exists.");

        var budget = new Budget
        {
            TenantId = request.TenantId,
            DepartmentId = request.DepartmentId,
            DepartmentName = request.DepartmentName,
            AcademicYear = request.AcademicYear,
            TotalAllocated = request.Lines.Sum(l => l.AllocatedAmount),
            TotalSpent = 0m,
            IsLocked = false
        };

        foreach (var line in request.Lines)
        {
            budget.Lines.Add(new BudgetLine
            {
                TenantId = request.TenantId,
                BudgetId = budget.Id,
                AccountId = line.AccountId,
                AccountName = line.AccountName,
                AllocatedAmount = line.AllocatedAmount,
                SpentAmount = 0m
            });
        }

        _db.Budgets.Add(budget);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(budget.Id);
    }
}
