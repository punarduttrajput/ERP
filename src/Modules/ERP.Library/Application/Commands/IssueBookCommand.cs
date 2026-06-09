using ERP.Library.Domain;
using ERP.Library.Infrastructure;
using ERP.Shared.Application.Common;
using ERP.Shared.Application.Contracts;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Library.Application.Commands;

public record IssueBookResult(Guid IssueId, DateOnly DueDate);

public record IssueBookCommand(
    string Barcode,
    Guid MemberId,
    MemberType MemberType,
    string MemberName,
    DateTime IssuedAt,
    Guid TenantId
) : IRequest<Result<IssueBookResult>>;

public class IssueBookCommandHandler : IRequestHandler<IssueBookCommand, Result<IssueBookResult>>
{
    private readonly ILibraryDbContext _db;
    private readonly IFeeService _feeService;

    public IssueBookCommandHandler(ILibraryDbContext db, IFeeService feeService)
    {
        _db = db;
        _feeService = feeService;
    }

    public async Task<Result<IssueBookResult>> Handle(IssueBookCommand request, CancellationToken cancellationToken)
    {
        var copy = await _db.BookCopies
            .Include(x => x.Book)
            .FirstOrDefaultAsync(x => x.Barcode == request.Barcode, cancellationToken);

        if (copy is null)
            return Result<IssueBookResult>.Failure("Book copy not found.");

        if (copy.Status != CopyStatus.Available)
            return Result<IssueBookResult>.Failure("Book copy is not available for issue.");

        var policy = await _db.LibraryPolicies
            .FirstOrDefaultAsync(x => x.MemberType == request.MemberType, cancellationToken);

        // Fall back to defaults when no policy has been configured for this member type yet
        policy ??= new LibraryPolicy
        {
            TenantId = request.TenantId,
            MemberType = request.MemberType,
            MaxBooksAllowed = 3,
            LoanPeriodDays = 14,
            FinePerDay = 2,
            MaxRenewals = 1,
            GracePeriodDays = 0
        };

        var activeCount = await _db.BookIssues
            .CountAsync(x => x.MemberId == request.MemberId
                && (x.Status == IssueStatus.Active || x.Status == IssueStatus.Overdue),
                cancellationToken);

        if (activeCount >= policy.MaxBooksAllowed)
            return Result<IssueBookResult>.Failure(
                $"Member has reached the maximum concurrent issue limit of {policy.MaxBooksAllowed}.");

        // IFeeService.HasDues is not on the interface; GetOutstandingBalanceAsync > 0 is the equivalent check.
        // Only applicable for Students — faculty/staff fees are not tracked through this service.
        if (request.MemberType == MemberType.Student)
        {
            var outstanding = await _feeService.GetOutstandingBalanceAsync(request.MemberId, cancellationToken);
            if (outstanding > 0)
                return Result<IssueBookResult>.Failure("Cannot issue: outstanding fee dues.");
        }

        copy.Status = CopyStatus.Issued;
        copy.Book!.AvailableCopies--;

        var dueDate = DateOnly.FromDateTime(request.IssuedAt.AddDays(policy.LoanPeriodDays));

        var issue = new BookIssue
        {
            TenantId = request.TenantId,
            CopyId = copy.Id,
            BookId = copy.BookId,
            BookTitle = copy.Book.Title,
            MemberId = request.MemberId,
            MemberType = request.MemberType,
            MemberName = request.MemberName,
            IssuedAt = request.IssuedAt,
            DueDate = dueDate,
            Status = IssueStatus.Active,
            RenewCount = 0
        };

        _db.BookIssues.Add(issue);
        await _db.SaveChangesAsync(cancellationToken);

        return Result<IssueBookResult>.Success(new IssueBookResult(issue.Id, dueDate));
    }
}
