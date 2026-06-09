using ERP.Finance.Domain;
using ERP.Finance.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;

namespace ERP.Finance.Application.Commands;

public record BankStatementLineInput(
    DateOnly TransactionDate,
    string Description,
    decimal Debit,
    decimal Credit,
    decimal Balance
);

public record ImportBankStatementCommand(
    Guid TenantId,
    Guid AccountId,
    string BankName,
    string AccountNumber,
    DateOnly StatementDate,
    decimal OpeningBalance,
    decimal ClosingBalance,
    IReadOnlyList<BankStatementLineInput> Lines
) : IRequest<Result<Guid>>;

public class ImportBankStatementHandler : IRequestHandler<ImportBankStatementCommand, Result<Guid>>
{
    private readonly IFinanceDbContext _db;

    public ImportBankStatementHandler(IFinanceDbContext db)
    {
        _db = db;
    }

    public async Task<Result<Guid>> Handle(ImportBankStatementCommand request, CancellationToken cancellationToken)
    {
        var statement = new BankStatement
        {
            TenantId = request.TenantId,
            AccountId = request.AccountId,
            BankName = request.BankName,
            AccountNumber = request.AccountNumber,
            StatementDate = request.StatementDate,
            OpeningBalance = request.OpeningBalance,
            ClosingBalance = request.ClosingBalance
        };

        foreach (var line in request.Lines)
        {
            statement.Lines.Add(new BankStatementLine
            {
                TenantId = request.TenantId,
                StatementId = statement.Id,
                TransactionDate = line.TransactionDate,
                Description = line.Description,
                Debit = line.Debit,
                Credit = line.Credit,
                Balance = line.Balance,
                ReconciliationStatus = ReconciliationStatus.Unreconciled
            });
        }

        _db.BankStatements.Add(statement);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(statement.Id);
    }
}
