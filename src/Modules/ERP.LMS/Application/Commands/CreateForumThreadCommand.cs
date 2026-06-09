using ERP.LMS.Domain;
using ERP.LMS.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace ERP.LMS.Application.Commands;

public record CreateForumThreadCommand(
    Guid TenantId,
    Guid SubjectId,
    Guid BatchId,
    string Title,
    string Body,
    Guid AuthorId) : IRequest<Result<Guid>>;

public class CreateForumThreadHandler : IRequestHandler<CreateForumThreadCommand, Result<Guid>>
{
    private readonly ILmsDbContext _db;
    private readonly IHubContext<ERP.LMS.API.Hubs.ForumHub> _hub;

    public CreateForumThreadHandler(ILmsDbContext db, IHubContext<ERP.LMS.API.Hubs.ForumHub> hub)
    {
        _db  = db;
        _hub = hub;
    }

    public async Task<Result<Guid>> Handle(CreateForumThreadCommand cmd, CancellationToken ct)
    {
        var thread = new ForumThread
        {
            TenantId  = cmd.TenantId,
            SubjectId = cmd.SubjectId,
            BatchId   = cmd.BatchId,
            Title     = cmd.Title,
            Body      = cmd.Body,
            AuthorId  = cmd.AuthorId
        };

        _db.ForumThreads.Add(thread);
        await _db.SaveChangesAsync(ct);

        await _hub.Clients.Group($"forum-{cmd.SubjectId}")
            .SendAsync("NewThread", new { threadId = thread.Id, title = thread.Title, authorId = thread.AuthorId }, ct);

        return Result.Success(thread.Id);
    }
}
