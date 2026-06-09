using ERP.LMS.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.LMS.Application.Queries;

public record AnnouncementDto(
    Guid Id,
    Guid SubjectId,
    Guid BatchId,
    string Title,
    string Body,
    Guid PostedBy,
    bool IsVisible,
    DateTime CreatedAt);

public record GetAnnouncementsQuery(Guid SubjectId, Guid BatchId) : IRequest<Result<IReadOnlyList<AnnouncementDto>>>;

public class GetAnnouncementsHandler : IRequestHandler<GetAnnouncementsQuery, Result<IReadOnlyList<AnnouncementDto>>>
{
    private readonly ILmsDbContext _db;

    public GetAnnouncementsHandler(ILmsDbContext db) => _db = db;

    public async Task<Result<IReadOnlyList<AnnouncementDto>>> Handle(GetAnnouncementsQuery query, CancellationToken ct)
    {
        var items = await _db.Announcements
            .Where(a => a.SubjectId == query.SubjectId && a.BatchId == query.BatchId && a.IsVisible && !a.IsDeleted)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new AnnouncementDto(a.Id, a.SubjectId, a.BatchId, a.Title, a.Body, a.PostedBy, a.IsVisible, a.CreatedAt))
            .ToListAsync(ct);

        return Result.Success<IReadOnlyList<AnnouncementDto>>(items);
    }
}
