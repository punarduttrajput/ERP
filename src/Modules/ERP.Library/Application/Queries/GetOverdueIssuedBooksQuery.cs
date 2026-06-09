using ERP.Library.Domain;
using ERP.Library.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Library.Application.Queries;

public record GetOverdueIssuedBooksQuery : IRequest<IReadOnlyList<IssueDto>>;

public class GetOverdueIssuedBooksQueryHandler : IRequestHandler<GetOverdueIssuedBooksQuery, IReadOnlyList<IssueDto>>
{
    private readonly ILibraryDbContext _db;

    public GetOverdueIssuedBooksQueryHandler(ILibraryDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<IssueDto>> Handle(GetOverdueIssuedBooksQuery request, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var issues = await _db.BookIssues
            .Include(x => x.Copy)
            .Where(x => !x.IsDeleted
                && (x.Status == IssueStatus.Active || x.Status == IssueStatus.Overdue)
                && x.DueDate < today)
            .OrderBy(x => x.DueDate)
            .ToListAsync(cancellationToken);

        return issues.Select(x => new IssueDto(
            x.Id, x.CopyId, x.BookTitle, x.Copy!.Barcode,
            x.MemberId, x.MemberName, x.MemberType,
            x.IssuedAt, x.DueDate, x.ReturnedAt, x.Status, x.RenewCount
        )).ToList();
    }
}
