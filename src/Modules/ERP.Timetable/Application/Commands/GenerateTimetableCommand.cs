using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Common;
using ERP.Timetable.Application.Services;
using ERP.Timetable.Domain;
using ERP.Timetable.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Timetable.Application.Commands;

public record GenerateTimetableCommand(Guid SemesterId, Guid BatchId) : IRequest<Result<int>>;

public class GenerateTimetableHandler : IRequestHandler<GenerateTimetableCommand, Result<int>>
{
    private readonly ITimetableDbContext _db;
    private readonly TimetableGeneratorService _generator;
    private readonly ICurrentTenant _currentTenant;

    public GenerateTimetableHandler(
        ITimetableDbContext db,
        TimetableGeneratorService generator,
        ICurrentTenant currentTenant)
    {
        _db = db;
        _generator = generator;
        _currentTenant = currentTenant;
    }

    public async Task<Result<int>> Handle(GenerateTimetableCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _currentTenant.TenantId!.Value;

        // Remove existing Draft entries to allow clean regeneration
        var drafts = await _db.TimetableEntries
            .Where(x =>
                x.TenantId == tenantId &&
                x.SemesterId == request.SemesterId &&
                x.BatchId == request.BatchId &&
                x.Status == TimetableStatus.Draft)
            .ToListAsync(cancellationToken);

        _db.TimetableEntries.RemoveRange(drafts);
        await _db.SaveChangesAsync(cancellationToken);

        return await _generator.GenerateAsync(request.SemesterId, request.BatchId, tenantId, cancellationToken);
    }
}
