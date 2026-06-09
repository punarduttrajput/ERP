using ERP.LMS.Domain;
using ERP.LMS.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;

namespace ERP.LMS.Application.Commands;

public record PostAnnouncementCommand(
    Guid TenantId,
    Guid SubjectId,
    Guid BatchId,
    string Title,
    string Body,
    Guid PostedBy) : IRequest<Result<Guid>>;

public class PostAnnouncementHandler : IRequestHandler<PostAnnouncementCommand, Result<Guid>>
{
    private readonly ILmsDbContext _db;

    public PostAnnouncementHandler(ILmsDbContext db) => _db = db;

    public async Task<Result<Guid>> Handle(PostAnnouncementCommand cmd, CancellationToken ct)
    {
        var announcement = new Announcement
        {
            TenantId  = cmd.TenantId,
            SubjectId = cmd.SubjectId,
            BatchId   = cmd.BatchId,
            Title     = cmd.Title,
            Body      = cmd.Body,
            PostedBy  = cmd.PostedBy,
            IsVisible = true
        };

        _db.Announcements.Add(announcement);
        await _db.SaveChangesAsync(ct);
        return Result.Success(announcement.Id);
    }
}
