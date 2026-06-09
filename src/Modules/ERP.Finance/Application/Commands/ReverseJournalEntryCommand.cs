using ERP.Finance.Domain;
using ERP.Finance.Infrastructure;
using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Finance.Application.Commands;

public record ReverseJournalEntryCommand(
    Guid TenantId,
    Guid EntryId,
    DateOnly ReverseDate,
    string Reason,
    Guid? ReversedBy
) : IRequest<Result<Guid>>;

public class ReverseJournalEntryHandler : IRequestHandler<ReverseJournalEntryCommand, Result<Guid>>
{
    private readonly IFinanceDbContext _db;
    private readonly IMediator _mediator;

    public ReverseJournalEntryHandler(IFinanceDbContext db, IMediator mediator)
    {
        _db = db;
        _mediator = mediator;
    }

    public async Task<Result<Guid>> Handle(ReverseJournalEntryCommand request, CancellationToken cancellationToken)
    {
        var original = await _db.JournalEntries
            .Include(e => e.Lines)
            .FirstOrDefaultAsync(e => e.Id == request.EntryId && e.TenantId == request.TenantId, cancellationToken);

        if (original is null)
            return Result.Failure<Guid>("Journal entry not found.");

        if (original.Status != EntryStatus.Posted)
            return Result.Failure<Guid>("Only Posted entries can be reversed.");

        var reversalLines = original.Lines.Select(l => new JournalLineInput(
            l.AccountId,
            l.Credit,  // swap: original credit becomes debit
            l.Debit,   // swap: original debit becomes credit
            l.Narration
        )).ToList();

        var createResult = await _mediator.Send(new CreateJournalEntryCommand(
            request.TenantId,
            request.ReverseDate,
            $"Reversal: {original.Description} — {request.Reason}",
            original.EntryNumber,
            reversalLines
        ), cancellationToken);

        if (!createResult.IsSuccess)
            return Result.Failure<Guid>(createResult.Error!);

        var reversalId = createResult.Value;

        var postResult = await _mediator.Send(new PostJournalEntryCommand(
            request.TenantId,
            reversalId,
            request.ReversedBy
        ), cancellationToken);

        if (!postResult.IsSuccess)
            return Result.Failure<Guid>(postResult.Error!);

        original.Status = EntryStatus.Reversed;
        original.ReversedByEntryId = reversalId;
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(reversalId);
    }
}
