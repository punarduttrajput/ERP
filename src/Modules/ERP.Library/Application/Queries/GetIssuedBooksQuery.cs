using ERP.Library.Domain;
using ERP.Library.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Library.Application.Queries;

public record IssueDto(
    Guid Id,
    Guid CopyId,
    string BookTitle,
    string Barcode,
    Guid MemberId,
    string MemberName,
    MemberType MemberType,
    DateTime IssuedAt,
    DateOnly DueDate,
    DateTime? ReturnedAt,
    IssueStatus Status,
    int RenewCount
);

public record GetIssuedBooksQuery(
    Guid? MemberId,
    IssueStatus? Status
) : IRequest<IReadOnlyList<IssueDto>>;

public class GetIssuedBooksQueryHandler : IRequestHandler<GetIssuedBooksQuery, IReadOnlyList<IssueDto>>
{
    private readonly ILibraryDbContext _db;

    public GetIssuedBooksQueryHandler(ILibraryDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<IssueDto>> Handle(GetIssuedBooksQuery request, CancellationToken cancellationToken)
    {
        var query = _db.BookIssues
            .Include(x => x.Copy)
            .Where(x => !x.IsDeleted);

        if (request.MemberId.HasValue)
            query = query.Where(x => x.MemberId == request.MemberId.Value);

        if (request.Status.HasValue)
            query = query.Where(x => x.Status == request.Status.Value);

        var issues = await query
            .OrderByDescending(x => x.IssuedAt)
            .ToListAsync(cancellationToken);

        return issues.Select(x => new IssueDto(
            x.Id, x.CopyId, x.BookTitle, x.Copy!.Barcode,
            x.MemberId, x.MemberName, x.MemberType,
            x.IssuedAt, x.DueDate, x.ReturnedAt, x.Status, x.RenewCount
        )).ToList();
    }
}
