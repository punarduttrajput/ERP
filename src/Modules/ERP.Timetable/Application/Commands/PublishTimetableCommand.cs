using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Common;
using ERP.Timetable.API.Hubs;
using ERP.Timetable.Domain;
using ERP.Timetable.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Timetable.Application.Commands;

public record PublishTimetableCommand(Guid SemesterId, Guid BatchId) : IRequest<Result>;

public class PublishTimetableHandler : IRequestHandler<PublishTimetableCommand, Result>
{
    private readonly ITimetableDbContext _db;
    private readonly ICurrentTenant _currentTenant;
    private readonly IHubContext<TimetableHub> _hubContext;

    public PublishTimetableHandler(
        ITimetableDbContext db,
        ICurrentTenant currentTenant,
        IHubContext<TimetableHub> hubContext)
    {
        _db = db;
        _currentTenant = currentTenant;
        _hubContext = hubContext;
    }

    public async Task<Result> Handle(PublishTimetableCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _currentTenant.TenantId!.Value;

        var entries = await _db.TimetableEntries
            .Where(x =>
                x.TenantId == tenantId &&
                x.SemesterId == request.SemesterId &&
                x.BatchId == request.BatchId &&
                x.Status == TimetableStatus.Draft)
            .ToListAsync(cancellationToken);

        if (entries.Count == 0)
        {
            // Idempotent: if already published, treat as success
            var alreadyPublished = await _db.TimetableEntries.AnyAsync(x =>
                x.TenantId == tenantId &&
                x.SemesterId == request.SemesterId &&
                x.BatchId == request.BatchId &&
                x.Status == TimetableStatus.Published, cancellationToken);

            if (alreadyPublished)
                return Result.Success();

            return Result.Failure("No draft timetable entries found to publish.");
        }

        foreach (var entry in entries)
            entry.Status = TimetableStatus.Published;

        await _db.SaveChangesAsync(cancellationToken);

        await _hubContext.Clients
            .Group($"timetable-{request.SemesterId}")
            .SendAsync("TimetablePublished", new
            {
                semesterId = request.SemesterId,
                batchId = request.BatchId,
                publishedAt = DateTime.UtcNow
            }, cancellationToken);

        return Result.Success();
    }
}
