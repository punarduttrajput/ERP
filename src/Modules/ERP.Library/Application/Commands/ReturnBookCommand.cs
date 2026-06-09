using ERP.Library.Domain;
using ERP.Library.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Library.Application.Commands;

public record ReturnBookResult(Guid IssueId, Fine? Fine);

public record ReturnBookCommand(
    string Barcode,
    DateTime ReturnedAt,
    Guid TenantId
) : IRequest<Result<ReturnBookResult>>;

public class ReturnBookCommandHandler : IRequestHandler<ReturnBookCommand, Result<ReturnBookResult>>
{
    private readonly ILibraryDbContext _db;

    public ReturnBookCommandHandler(ILibraryDbContext db)
    {
        _db = db;
    }

    public async Task<Result<ReturnBookResult>> Handle(ReturnBookCommand request, CancellationToken cancellationToken)
    {
        var copy = await _db.BookCopies
            .Include(x => x.Book)
            .FirstOrDefaultAsync(x => x.Barcode == request.Barcode, cancellationToken);

        if (copy is null)
            return Result<ReturnBookResult>.Failure("Book copy not found.");

        var issue = await _db.BookIssues
            .FirstOrDefaultAsync(x => x.CopyId == copy.Id
                && (x.Status == IssueStatus.Active || x.Status == IssueStatus.Overdue),
                cancellationToken);

        if (issue is null)
            return Result<ReturnBookResult>.Failure("No active issue found for this copy.");

        var policy = await _db.LibraryPolicies
            .FirstOrDefaultAsync(x => x.MemberType == issue.MemberType, cancellationToken);

        var gracePeriodDays = policy?.GracePeriodDays ?? 0;
        var finePerDay = policy?.FinePerDay ?? 2m;

        issue.ReturnedAt = request.ReturnedAt;
        issue.Status = IssueStatus.Returned;

        copy.Status = CopyStatus.Available;
        copy.Book!.AvailableCopies++;

        var returnDate = DateOnly.FromDateTime(request.ReturnedAt);
        var daysOverdue = Math.Max(0,
            returnDate.DayNumber - issue.DueDate.DayNumber - gracePeriodDays);

        Fine? fine = null;
        if (daysOverdue > 0)
        {
            fine = new Fine
            {
                TenantId = request.TenantId,
                IssueId = issue.Id,
                MemberId = issue.MemberId,
                DaysOverdue = daysOverdue,
                FinePerDay = finePerDay,
                TotalFine = daysOverdue * finePerDay,
                Status = FineStatus.Pending
            };

            _db.LibraryFines.Add(fine);
        }

        await _db.SaveChangesAsync(cancellationToken);

        return Result<ReturnBookResult>.Success(new ReturnBookResult(issue.Id, fine));
    }
}
