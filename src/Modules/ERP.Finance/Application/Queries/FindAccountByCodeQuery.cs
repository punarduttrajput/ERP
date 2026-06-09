using ERP.Finance.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Finance.Application.Queries;

public record FindAccountByCodeQuery(Guid TenantId, string Code) : IRequest<Result<Guid>>;

public class FindAccountByCodeHandler : IRequestHandler<FindAccountByCodeQuery, Result<Guid>>
{
    private readonly IFinanceDbContext _db;

    public FindAccountByCodeHandler(IFinanceDbContext db)
    {
        _db = db;
    }

    public async Task<Result<Guid>> Handle(FindAccountByCodeQuery request, CancellationToken cancellationToken)
    {
        var account = await _db.GlAccounts
            .Where(a => a.TenantId == request.TenantId && a.Code == request.Code && a.IsActive)
            .Select(a => new { a.Id })
            .FirstOrDefaultAsync(cancellationToken);

        if (account is null)
            return Result.Failure<Guid>($"Account with code '{request.Code}' not found.");

        return Result.Success(account.Id);
    }
}
