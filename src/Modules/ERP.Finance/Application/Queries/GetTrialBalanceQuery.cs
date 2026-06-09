using Dapper;
using ERP.Finance.Domain;
using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Common;
using MediatR;

namespace ERP.Finance.Application.Queries;

public record TrialBalanceLineDto(
    string Code,
    string Name,
    AccountType AccountType,
    decimal TotalDebit,
    decimal TotalCredit,
    decimal Balance
);

public record GetTrialBalanceQuery(Guid TenantId, DateOnly FromDate, DateOnly ToDate) : IRequest<Result<List<TrialBalanceLineDto>>>;

public class GetTrialBalanceHandler : IRequestHandler<GetTrialBalanceQuery, Result<List<TrialBalanceLineDto>>>
{
    private readonly IDbConnectionFactory _connectionFactory;

    public GetTrialBalanceHandler(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Result<List<TrialBalanceLineDto>>> Handle(GetTrialBalanceQuery request, CancellationToken cancellationToken)
    {
        using var conn = await _connectionFactory.CreateReadConnectionAsync(cancellationToken);

        const string sql = @"
SELECT
    a.Code, a.Name, a.AccountType,
    COALESCE(SUM(jl.Debit), 0) AS TotalDebit,
    COALESCE(SUM(jl.Credit), 0) AS TotalCredit,
    a.Balance
FROM gl_accounts a
LEFT JOIN journal_lines jl ON jl.AccountId = a.Id
    AND jl.IsDeleted = 0
LEFT JOIN journal_entries je ON je.Id = jl.JournalEntryId
    AND je.Status = 1
    AND je.TenantId = @TenantId
    AND je.EntryDate BETWEEN @FromDate AND @ToDate
WHERE a.TenantId = @TenantId AND a.IsActive = 1
GROUP BY a.Id, a.Code, a.Name, a.AccountType, a.Balance
ORDER BY a.Code";

        var rows = await conn.QueryAsync<TrialBalanceLineDto>(sql, new
        {
            TenantId = request.TenantId,
            FromDate = request.FromDate,
            ToDate = request.ToDate
        });

        return Result.Success(rows.ToList());
    }
}
