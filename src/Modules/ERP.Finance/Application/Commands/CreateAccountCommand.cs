using ERP.Finance.Domain;
using ERP.Finance.Infrastructure;
using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Common;
using MediatR;

namespace ERP.Finance.Application.Commands;

public record CreateAccountCommand(
    Guid TenantId,
    string Code,
    string Name,
    AccountType AccountType,
    Guid? ParentAccountId,
    bool IsControl
) : IRequest<Result<Guid>>;

public class CreateAccountHandler : IRequestHandler<CreateAccountCommand, Result<Guid>>
{
    private readonly IFinanceDbContext _db;

    public CreateAccountHandler(IFinanceDbContext db)
    {
        _db = db;
    }

    public async Task<Result<Guid>> Handle(CreateAccountCommand request, CancellationToken cancellationToken)
    {
        var duplicate = _db.GlAccounts.Any(a => a.TenantId == request.TenantId && a.Code == request.Code);
        if (duplicate)
            return Result.Failure<Guid>($"Account with code '{request.Code}' already exists.");

        var account = new Account
        {
            TenantId = request.TenantId,
            Code = request.Code,
            Name = request.Name,
            AccountType = request.AccountType,
            ParentAccountId = request.ParentAccountId,
            IsControl = request.IsControl,
            IsActive = true,
            Balance = 0m
        };

        _db.GlAccounts.Add(account);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(account.Id);
    }
}
