using ERP.Compliance.Domain;
using ERP.Compliance.Infrastructure;
using ERP.Shared.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERP.Compliance.Application.Jobs;

public class ComplianceAlertJob
{
    private readonly IComplianceDbContext _db;
    private readonly IEmailService _emailService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<ComplianceAlertJob> _logger;

    public ComplianceAlertJob(
        IComplianceDbContext db,
        IEmailService emailService,
        ICacheService cacheService,
        ILogger<ComplianceAlertJob> logger)
    {
        _db = db;
        _emailService = emailService;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var cutoff = today.AddDays(30);
        var todayStr = today.ToString("yyyy-MM-dd");

        // IgnoreQueryFilters: Hangfire jobs execute without an HTTP request,
        // so ICurrentTenant has no tenant context and the global query filter
        // would resolve to Guid.Empty, silently returning zero rows for all tenants.
        var dueSoonItems = await _db.ComplianceItems
            .IgnoreQueryFilters()
            .Where(x => !x.IsDeleted
                        && x.DueDate >= today
                        && x.DueDate <= cutoff
                        && (x.Status == ComplianceStatus.Pending || x.Status == ComplianceStatus.InProgress))
            .ToListAsync(cancellationToken);

        foreach (var item in dueSoonItems)
        {
            var cacheKey = $"comp_alert:{item.TenantId}:{item.Id}:{todayStr}:DueSoon";
            if (await _cacheService.ExistsAsync(cacheKey, cancellationToken))
                continue;

            _db.ComplianceNotifications.Add(new ComplianceNotification
            {
                TenantId = item.TenantId,
                ComplianceItemId = item.Id,
                RecipientUserId = item.ResponsiblePersonId ?? Guid.Empty,
                Message = $"Compliance item '{item.Title}' ({item.Authority}) is due on {item.DueDate:dd MMM yyyy}.",
                SentAt = DateTime.UtcNow,
                NotificationType = "DueSoon"
            });

            if (item.ResponsiblePersonId.HasValue && !string.IsNullOrWhiteSpace(item.ResponsiblePersonName))
            {
                try
                {
                    await _emailService.SendAsync(
                        to: $"{item.ResponsiblePersonId}@placeholder.erp",
                        subject: $"[Compliance Alert] '{item.Title}' due {item.DueDate:dd MMM yyyy}",
                        htmlBody: $"<p>Dear {item.ResponsiblePersonName},</p>" +
                                  $"<p>The compliance item <strong>{item.Title}</strong> ({item.Authority}) is due on <strong>{item.DueDate:dd MMM yyyy}</strong>. Please take the necessary action.</p>",
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send DueSoon email for compliance item {ItemId}", item.Id);
                }
            }

            await _cacheService.SetAsync(cacheKey, new AlertSent(item.Id), TimeSpan.FromHours(25), cancellationToken);
        }

        var overdueItems = await _db.ComplianceItems
            .IgnoreQueryFilters()
            .Where(x => !x.IsDeleted
                        && x.DueDate < today
                        && x.Status != ComplianceStatus.Completed
                        && x.Status != ComplianceStatus.NotApplicable)
            .ToListAsync(cancellationToken);

        foreach (var item in overdueItems)
        {
            item.Status = ComplianceStatus.Overdue;
            item.UpdatedAt = DateTime.UtcNow;

            var cacheKey = $"comp_alert:{item.TenantId}:{item.Id}:{todayStr}:Overdue";
            if (await _cacheService.ExistsAsync(cacheKey, cancellationToken))
                continue;

            _db.ComplianceNotifications.Add(new ComplianceNotification
            {
                TenantId = item.TenantId,
                ComplianceItemId = item.Id,
                RecipientUserId = item.ResponsiblePersonId ?? Guid.Empty,
                Message = $"Compliance item '{item.Title}' ({item.Authority}) is OVERDUE. Due date was {item.DueDate:dd MMM yyyy}.",
                SentAt = DateTime.UtcNow,
                NotificationType = "Overdue"
            });

            if (item.ResponsiblePersonId.HasValue && !string.IsNullOrWhiteSpace(item.ResponsiblePersonName))
            {
                try
                {
                    await _emailService.SendAsync(
                        to: $"{item.ResponsiblePersonId}@placeholder.erp",
                        subject: $"[Compliance OVERDUE] '{item.Title}' was due {item.DueDate:dd MMM yyyy}",
                        htmlBody: $"<p>Dear {item.ResponsiblePersonName},</p>" +
                                  $"<p>The compliance item <strong>{item.Title}</strong> ({item.Authority}) was due on <strong>{item.DueDate:dd MMM yyyy}</strong> and is now <strong>OVERDUE</strong>. Please take immediate action.</p>",
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send Overdue email for compliance item {ItemId}", item.Id);
                }
            }

            await _cacheService.SetAsync(cacheKey, new AlertSent(item.Id), TimeSpan.FromHours(25), cancellationToken);
        }

        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("ComplianceAlertJob completed. DueSoon: {DueSoon}, Overdue: {Overdue}", dueSoonItems.Count, overdueItems.Count);
    }

    private record AlertSent(Guid ItemId);
}
