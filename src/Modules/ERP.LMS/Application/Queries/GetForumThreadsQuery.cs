using ERP.LMS.Domain;
using ERP.LMS.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.LMS.Application.Queries;

public record ForumReplyDto(
    Guid Id,
    Guid ThreadId,
    Guid AuthorId,
    string Body,
    DateTime CreatedAt);

public record ForumThreadSummaryDto(
    Guid Id,
    Guid SubjectId,
    Guid BatchId,
    string Title,
    string Body,
    Guid AuthorId,
    bool IsPinned,
    int ReplyCount,
    DateTime CreatedAt);

public record ForumThreadDetailDto(
    Guid Id,
    Guid SubjectId,
    Guid BatchId,
    string Title,
    string Body,
    Guid AuthorId,
    bool IsPinned,
    int ReplyCount,
    DateTime CreatedAt,
    IReadOnlyList<ForumReplyDto> Replies);

public record GetForumThreadsQuery(Guid SubjectId, Guid BatchId, int Page = 1, int PageSize = 20) : IRequest<Result<PagedResult<ForumThreadSummaryDto>>>;

public class GetForumThreadsHandler : IRequestHandler<GetForumThreadsQuery, Result<PagedResult<ForumThreadSummaryDto>>>
{
    private readonly ILmsDbContext _db;

    public GetForumThreadsHandler(ILmsDbContext db) => _db = db;

    public async Task<Result<PagedResult<ForumThreadSummaryDto>>> Handle(GetForumThreadsQuery query, CancellationToken ct)
    {
        var baseQuery = _db.ForumThreads
            .Where(t => t.SubjectId == query.SubjectId && t.BatchId == query.BatchId && !t.IsDeleted)
            .OrderByDescending(t => t.IsPinned)
            .ThenByDescending(t => t.CreatedAt);

        var total = await baseQuery.CountAsync(ct);
        var items = await baseQuery
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(t => new ForumThreadSummaryDto(t.Id, t.SubjectId, t.BatchId, t.Title, t.Body, t.AuthorId, t.IsPinned, t.ReplyCount, t.CreatedAt))
            .ToListAsync(ct);

        return Result.Success(new PagedResult<ForumThreadSummaryDto>(items, total, query.Page, query.PageSize));
    }
}

public record GetForumThreadDetailQuery(Guid ThreadId) : IRequest<Result<ForumThreadDetailDto>>;

public class GetForumThreadDetailHandler : IRequestHandler<GetForumThreadDetailQuery, Result<ForumThreadDetailDto>>
{
    private readonly ILmsDbContext _db;

    public GetForumThreadDetailHandler(ILmsDbContext db) => _db = db;

    public async Task<Result<ForumThreadDetailDto>> Handle(GetForumThreadDetailQuery query, CancellationToken ct)
    {
        var thread = await _db.ForumThreads
            .Include(t => t.Replies.Where(r => !r.IsDeleted))
            .FirstOrDefaultAsync(t => t.Id == query.ThreadId && !t.IsDeleted, ct);

        if (thread is null)
            return Result.Failure<ForumThreadDetailDto>("Thread not found.");

        var replies = thread.Replies
            .OrderBy(r => r.CreatedAt)
            .Select(r => new ForumReplyDto(r.Id, r.ThreadId, r.AuthorId, r.Body, r.CreatedAt))
            .ToList();

        return Result.Success(new ForumThreadDetailDto(thread.Id, thread.SubjectId, thread.BatchId, thread.Title, thread.Body, thread.AuthorId, thread.IsPinned, thread.ReplyCount, thread.CreatedAt, replies));
    }
}
