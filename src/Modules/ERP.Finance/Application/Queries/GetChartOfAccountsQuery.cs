using ERP.Finance.Domain;
using ERP.Finance.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Finance.Application.Queries;

public record AccountDto(
    Guid Id,
    string Code,
    string Name,
    AccountType AccountType,
    Guid? ParentAccountId,
    bool IsControl,
    bool IsActive,
    decimal Balance,
    List<AccountDto> Children
);

public record GetChartOfAccountsQuery(Guid TenantId) : IRequest<Result<List<AccountDto>>>;

public class GetChartOfAccountsHandler : IRequestHandler<GetChartOfAccountsQuery, Result<List<AccountDto>>>
{
    private readonly IFinanceDbContext _db;

    public GetChartOfAccountsHandler(IFinanceDbContext db)
    {
        _db = db;
    }

    public async Task<Result<List<AccountDto>>> Handle(GetChartOfAccountsQuery request, CancellationToken cancellationToken)
    {
        var accounts = await _db.GlAccounts
            .Where(a => a.TenantId == request.TenantId && a.IsActive)
            .OrderBy(a => a.Code)
            .ToListAsync(cancellationToken);

        var all = accounts.ToDictionary(a => a.Id, a => new AccountDto(
            a.Id, a.Code, a.Name, a.AccountType, a.ParentAccountId, a.IsControl, a.IsActive, a.Balance, new List<AccountDto>()
        ));

        var roots = new List<AccountDto>();
        foreach (var dto in all.Values)
        {
            if (dto.ParentAccountId.HasValue && all.TryGetValue(dto.ParentAccountId.Value, out var parent))
                parent.Children.Add(dto);
            else
                roots.Add(dto);
        }

        return Result.Success(roots);
    }
}
