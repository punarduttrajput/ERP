using ERP.Finance.Domain;
using ERP.Finance.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Finance.Application.Commands;

public record ReconcileLineCommand(
    Guid TenantId,
    Guid BankStatementLineId,
    Guid JournalLineId
) : IRequest<Result>;

public class ReconcileLineHandler : IRequestHandler<ReconcileLineCommand, Result>
{
    private readonly IFinanceDbContext _db;

    public ReconcileLineHandler(IFinanceDbContext db)
    {
        _db = db;
    }

    public async Task<Result> Handle(ReconcileLineCommand request, CancellationToken cancellationToken)
    {
        var statementLine = await _db.BankStatementLines
            .FirstOrDefaultAsync(l => l.Id == request.BankStatementLineId && l.TenantId == request.TenantId, cancellationToken);

        if (statementLine is null)
            return Result.Failure("Bank statement line not found.");

        var journalLine = await _db.JournalLines
            .FirstOrDefaultAsync(l => l.Id == request.JournalLineId && l.TenantId == request.TenantId, cancellationToken);

        if (journalLine is null)
            return Result.Failure("Journal line not found.");

        statementLine.ReconciliationStatus = ReconciliationStatus.Reconciled;
        statementLine.MatchedJournalLineId = request.JournalLineId;

        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
