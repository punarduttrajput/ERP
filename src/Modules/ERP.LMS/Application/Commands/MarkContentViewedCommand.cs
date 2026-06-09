using ERP.LMS.Domain;
using ERP.LMS.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace ERP.LMS.Application.Commands;

public record MarkContentViewedCommand(
    Guid TenantId,
    Guid StudentId,
    Guid CourseContentId) : IRequest<Result>;

public class MarkContentViewedHandler : IRequestHandler<MarkContentViewedCommand, Result>
{
    private readonly ILmsDbContext _db;
    private readonly IConnectionMultiplexer _redis;

    public MarkContentViewedHandler(ILmsDbContext db, IConnectionMultiplexer redis)
    {
        _db    = db;
        _redis = redis;
    }

    public async Task<Result> Handle(MarkContentViewedCommand cmd, CancellationToken ct)
    {
        var content = await _db.CourseContents.FirstOrDefaultAsync(c => c.Id == cmd.CourseContentId, ct);
        if (content is null)
            return Result.Failure("Content not found.");

        // Idempotency guard: only count the view once per student per content item.
        // TTL of 1 year prevents unbounded key growth while covering any realistic academic session.
        var redisKey = $"content_viewed:{cmd.TenantId}:{cmd.StudentId}:{cmd.CourseContentId}";
        var db = _redis.GetDatabase();
        var wasSet = await db.StringSetAsync(redisKey, "1", TimeSpan.FromDays(365), When.NotExists);

        if (!wasSet)
            return Result.Success();

        var progress = await _db.StudentProgresses
            .FirstOrDefaultAsync(p => p.TenantId == cmd.TenantId && p.StudentId == cmd.StudentId && p.SubjectId == content.SubjectId && p.BatchId == content.BatchId, ct);

        if (progress is null)
        {
            progress = new StudentProgress
            {
                TenantId  = cmd.TenantId,
                StudentId = cmd.StudentId,
                SubjectId = content.SubjectId,
                BatchId   = content.BatchId
            };
            _db.StudentProgresses.Add(progress);
        }

        progress.ContentViewedCount++;
        progress.LastActivityAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
