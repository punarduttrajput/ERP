using ERP.LMS.Domain;
using ERP.LMS.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace ERP.LMS.Application.Commands;

public record ReplyToThreadCommand(
    Guid TenantId,
    Guid ThreadId,
    Guid AuthorId,
    string Body) : IRequest<Result<Guid>>;

public class ReplyToThreadHandler : IRequestHandler<ReplyToThreadCommand, Result<Guid>>
{
    private readonly ILmsDbContext _db;
    private readonly IHubContext<ERP.LMS.API.Hubs.ForumHub> _hub;

    public ReplyToThreadHandler(ILmsDbContext db, IHubContext<ERP.LMS.API.Hubs.ForumHub> hub)
    {
        _db  = db;
        _hub = hub;
    }

    public async Task<Result<Guid>> Handle(ReplyToThreadCommand cmd, CancellationToken ct)
    {
        var thread = await _db.ForumThreads.FirstOrDefaultAsync(t => t.Id == cmd.ThreadId, ct);
        if (thread is null)
            return Result.Failure<Guid>("Thread not found.");

        var reply = new ForumReply
        {
            TenantId = cmd.TenantId,
            ThreadId = cmd.ThreadId,
            AuthorId = cmd.AuthorId,
            Body     = cmd.Body
        };

        _db.ForumReplies.Add(reply);
        thread.ReplyCount++;

        await _db.SaveChangesAsync(ct);

        await _hub.Clients.Group($"forum-{thread.SubjectId}")
            .SendAsync("NewReply", new { threadId = thread.Id, replyId = reply.Id, authorId = reply.AuthorId }, ct);

        return Result.Success(reply.Id);
    }
}
