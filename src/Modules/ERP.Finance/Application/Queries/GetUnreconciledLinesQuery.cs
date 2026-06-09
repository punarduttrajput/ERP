using ERP.Finance.Domain;
using ERP.Finance.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Finance.Application.Queries;

public record UnreconciledLineDto(
    Guid Id,
    DateOnly TransactionDate,
    string Description,
    decimal Debit,
    decimal Credit,
    decimal Balance,
    ReconciliationStatus ReconciliationStatus
);

public record GetUnreconciledLinesQuery(Guid TenantId, Guid StatementId) : IRequest<Result<List<UnreconciledLineDto>>>;

public class GetUnreconciledLinesHandler : IRequestHandler<GetUnreconciledLinesQuery, Result<List<UnreconciledLineDto>>>
{
    private readonly IFinanceDbContext _db;

    public GetUnreconciledLinesHandler(IFinanceDbContext db)
    {
        _db = db;
    }

    public async Task<Result<List<UnreconciledLineDto>>> Handle(GetUnreconciledLinesQuery request, CancellationToken cancellationToken)
    {
        var lines = await _db.BankStatementLines
            .Where(l =>
                l.TenantId == request.TenantId &&
                l.StatementId == request.StatementId &&
                l.ReconciliationStatus == ReconciliationStatus.Unreconciled)
            .OrderBy(l => l.TransactionDate)
            .Select(l => new UnreconciledLineDto(
                l.Id,
                l.TransactionDate,
                l.Description,
                l.Debit,
                l.Credit,
                l.Balance,
                l.ReconciliationStatus
            ))
            .ToListAsync(cancellationToken);

        return Result.Success(lines);
    }
}
