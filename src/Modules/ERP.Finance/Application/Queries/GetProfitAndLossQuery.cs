using Dapper;
using ERP.Finance.Domain;
using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Common;
using MediatR;

namespace ERP.Finance.Application.Queries;

public record PnlLineDto(string Code, string Name, AccountType AccountType, decimal Amount);

public record ProfitAndLossDto(
    List<PnlLineDto> IncomeLines,
    List<PnlLineDto> ExpenseLines,
    decimal TotalIncome,
    decimal TotalExpenses,
    decimal NetProfit
);

public record GetProfitAndLossQuery(Guid TenantId, DateOnly FromDate, DateOnly ToDate) : IRequest<Result<ProfitAndLossDto>>;

public class GetProfitAndLossHandler : IRequestHandler<GetProfitAndLossQuery, Result<ProfitAndLossDto>>
{
    private readonly IDbConnectionFactory _connectionFactory;

    public GetProfitAndLossHandler(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Result<ProfitAndLossDto>> Handle(GetProfitAndLossQuery request, CancellationToken cancellationToken)
    {
        using var conn = await _connectionFactory.CreateReadConnectionAsync(cancellationToken);

        const string sql = @"
SELECT
    a.Code, a.Name, a.AccountType,
    CASE
        WHEN a.AccountType = 4 THEN COALESCE(SUM(jl.Credit - jl.Debit), 0)
        WHEN a.AccountType = 5 THEN COALESCE(SUM(jl.Debit - jl.Credit), 0)
    END AS Amount
FROM gl_accounts a
LEFT JOIN journal_lines jl ON jl.AccountId = a.Id AND jl.IsDeleted = 0
LEFT JOIN journal_entries je ON je.Id = jl.JournalEntryId
    AND je.Status = 1
    AND je.TenantId = @TenantId
    AND je.EntryDate BETWEEN @FromDate AND @ToDate
WHERE a.TenantId = @TenantId
    AND a.IsActive = 1
    AND a.AccountType IN (4, 5)
GROUP BY a.Id, a.Code, a.Name, a.AccountType
ORDER BY a.Code";

        var rows = (await conn.QueryAsync<PnlLineDto>(sql, new
        {
            TenantId = request.TenantId,
            FromDate = request.FromDate,
            ToDate = request.ToDate
        })).ToList();

        var incomeLines = rows.Where(r => r.AccountType == AccountType.Income).ToList();
        var expenseLines = rows.Where(r => r.AccountType == AccountType.Expense).ToList();
        var totalIncome = incomeLines.Sum(r => r.Amount);
        var totalExpenses = expenseLines.Sum(r => r.Amount);

        return Result.Success(new ProfitAndLossDto(incomeLines, expenseLines, totalIncome, totalExpenses, totalIncome - totalExpenses));
    }
}
