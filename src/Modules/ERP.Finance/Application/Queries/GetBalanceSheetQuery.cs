using Dapper;
using ERP.Finance.Domain;
using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Common;
using MediatR;

namespace ERP.Finance.Application.Queries;

public record BalanceSheetLineDto(string Code, string Name, AccountType AccountType, decimal Balance);

public record BalanceSheetDto(
    List<BalanceSheetLineDto> Assets,
    List<BalanceSheetLineDto> Liabilities,
    List<BalanceSheetLineDto> Equity,
    decimal TotalAssets,
    decimal TotalLiabilities,
    decimal TotalEquity
);

public record GetBalanceSheetQuery(Guid TenantId, DateOnly AsOfDate) : IRequest<Result<BalanceSheetDto>>;

public class GetBalanceSheetHandler : IRequestHandler<GetBalanceSheetQuery, Result<BalanceSheetDto>>
{
    private readonly IDbConnectionFactory _connectionFactory;

    public GetBalanceSheetHandler(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Result<BalanceSheetDto>> Handle(GetBalanceSheetQuery request, CancellationToken cancellationToken)
    {
        using var conn = await _connectionFactory.CreateReadConnectionAsync(cancellationToken);

        // Point-in-time balances: use running Balance from Account, which reflects all posted entries up to now.
        // For true as-of-date accuracy, compute from journal history up to AsOfDate.
        const string sql = @"
SELECT
    a.Code, a.Name, a.AccountType,
    COALESCE(SUM(
        CASE
            WHEN a.AccountType = 1 THEN jl.Debit - jl.Credit
            WHEN a.AccountType IN (2, 3) THEN jl.Credit - jl.Debit
        END
    ), 0) AS Balance
FROM gl_accounts a
LEFT JOIN journal_lines jl ON jl.AccountId = a.Id AND jl.IsDeleted = 0
LEFT JOIN journal_entries je ON je.Id = jl.JournalEntryId
    AND je.Status = 1
    AND je.TenantId = @TenantId
    AND je.EntryDate <= @AsOfDate
WHERE a.TenantId = @TenantId
    AND a.IsActive = 1
    AND a.AccountType IN (1, 2, 3)
GROUP BY a.Id, a.Code, a.Name, a.AccountType
ORDER BY a.Code";

        var rows = (await conn.QueryAsync<BalanceSheetLineDto>(sql, new
        {
            TenantId = request.TenantId,
            AsOfDate = request.AsOfDate
        })).ToList();

        var assets = rows.Where(r => r.AccountType == AccountType.Asset).ToList();
        var liabilities = rows.Where(r => r.AccountType == AccountType.Liability).ToList();
        var equity = rows.Where(r => r.AccountType == AccountType.Equity).ToList();

        return Result.Success(new BalanceSheetDto(
            assets, liabilities, equity,
            assets.Sum(r => r.Balance),
            liabilities.Sum(r => r.Balance),
            equity.Sum(r => r.Balance)
        ));
    }
}
