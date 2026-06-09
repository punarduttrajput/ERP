using ERP.Library.Domain;
using ERP.Library.Infrastructure;
using ERP.Shared.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace ERP.Library.Application.Jobs;

public class OverdueNotificationJob
{
    private readonly ILibraryDbContext _db;
    private readonly ISmsService _smsService;
    private readonly IConnectionMultiplexer _redis;

    // Mobile number is not stored on BookIssue — SMS is sent via a best-effort lookup.
    // In production, resolve the mobile from SIS/HRMS using MemberId; here we send to a
    // placeholder because cross-module queries require a separate service dependency.
    private const string PlaceholderMobile = "0000000000";

    public OverdueNotificationJob(
        ILibraryDbContext db,
        ISmsService smsService,
        IConnectionMultiplexer redis)
    {
        _db = db;
        _smsService = smsService;
        _redis = redis;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var cache = _redis.GetDatabase();

        // IgnoreQueryFilters bypasses the global TenantId filter so the job processes all tenants
        // in a single pass. This is intentional — the job runs outside any HTTP request context
        // so ICurrentTenant returns null and would filter out all rows if the filter were active.
        var overdueIssues = await _db.BookIssues
            .IgnoreQueryFilters()
            .Where(x => !x.IsDeleted
                && (x.Status == IssueStatus.Active || x.Status == IssueStatus.Overdue)
                && x.DueDate < today)
            .ToListAsync(cancellationToken);

        foreach (var issue in overdueIssues)
        {
            // Deduplicate: skip if already notified for this issue today
            var cacheKey = $"lib_overdue_notified:{issue.TenantId}:{issue.Id}:{today:yyyyMMdd}";
            var alreadyNotified = await cache.StringGetAsync(cacheKey);
            if (alreadyNotified.HasValue)
                continue;

            var message =
                $"Dear {issue.MemberName}, your book '{issue.BookTitle}' was due on {issue.DueDate:dd MMM yyyy}. " +
                "Please return it to avoid fines.";

            await _smsService.SendAsync(PlaceholderMobile, message, cancellationToken);

            // Mark issue as Overdue and flag cache with 25-hour TTL to prevent same-day re-notification
            issue.Status = IssueStatus.Overdue;
            await cache.StringSetAsync(cacheKey, "1", TimeSpan.FromHours(25));
        }

        await _db.SaveChangesAsync(cancellationToken);
    }
}
