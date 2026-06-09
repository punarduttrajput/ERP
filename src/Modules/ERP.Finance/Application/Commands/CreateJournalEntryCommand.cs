using ERP.Finance.Domain;
using ERP.Finance.Infrastructure;
using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Finance.Application.Commands;

public record JournalLineInput(Guid AccountId, decimal Debit, decimal Credit, string? Narration);

public record CreateJournalEntryCommand(
    Guid TenantId,
    DateOnly EntryDate,
    string Description,
    string? Reference,
    IReadOnlyList<JournalLineInput> Lines
) : IRequest<Result<Guid>>;

public class CreateJournalEntryHandler : IRequestHandler<CreateJournalEntryCommand, Result<Guid>>
{
    private readonly IFinanceDbContext _db;

    public CreateJournalEntryHandler(IFinanceDbContext db)
    {
        _db = db;
    }

    public async Task<Result<Guid>> Handle(CreateJournalEntryCommand request, CancellationToken cancellationToken)
    {
        var totalDebit = request.Lines.Sum(l => l.Debit);
        var totalCredit = request.Lines.Sum(l => l.Credit);

        if (totalDebit != totalCredit)
            return Result.Failure<Guid>($"Journal entry is not balanced: total debit {totalDebit} != total credit {totalCredit}.");

        foreach (var line in request.Lines)
        {
            if (line.Debit > 0 && line.Credit > 0)
                return Result.Failure<Guid>("A journal line cannot have both debit and credit values.");
        }

        var accountIds = request.Lines.Select(l => l.AccountId).Distinct().ToList();
        var accounts = await _db.GlAccounts
            .Where(a => accountIds.Contains(a.Id) && a.TenantId == request.TenantId)
            .ToListAsync(cancellationToken);

        var accountMap = accounts.ToDictionary(a => a.Id);

        foreach (var line in request.Lines)
        {
            if (!accountMap.ContainsKey(line.AccountId))
                return Result.Failure<Guid>($"Account {line.AccountId} not found.");

            if (accountMap[line.AccountId].IsControl)
                return Result.Failure<Guid>($"Account '{accountMap[line.AccountId].Code}' is a control account and cannot receive direct postings.");
        }

        var seq = _db.JournalEntries.Count(e => e.TenantId == request.TenantId) + 1;
        var entryNumber = $"JE-{request.EntryDate.Year}-{seq:D6}";

        var entry = new JournalEntry
        {
            TenantId = request.TenantId,
            EntryNumber = entryNumber,
            EntryDate = request.EntryDate,
            Description = request.Description,
            Reference = request.Reference,
            Status = EntryStatus.Draft,
            TotalDebit = totalDebit,
            TotalCredit = totalCredit
        };

        foreach (var line in request.Lines)
        {
            var account = accountMap[line.AccountId];
            entry.Lines.Add(new JournalLine
            {
                TenantId = request.TenantId,
                JournalEntryId = entry.Id,
                AccountId = line.AccountId,
                AccountCode = account.Code,
                AccountName = account.Name,
                Debit = line.Debit,
                Credit = line.Credit,
                Narration = line.Narration
            });
        }

        _db.JournalEntries.Add(entry);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(entry.Id);
    }
}
