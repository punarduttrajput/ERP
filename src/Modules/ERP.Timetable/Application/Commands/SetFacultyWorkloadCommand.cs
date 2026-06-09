using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Common;
using ERP.Timetable.Domain;
using ERP.Timetable.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Timetable.Application.Commands;

public record SetFacultyWorkloadCommand(
    Guid FacultyUserId,
    Guid SemesterId,
    int MaxHoursPerWeek) : IRequest<Result<Guid>>;

public class SetFacultyWorkloadHandler : IRequestHandler<SetFacultyWorkloadCommand, Result<Guid>>
{
    private readonly ITimetableDbContext _db;
    private readonly ICurrentTenant _currentTenant;

    public SetFacultyWorkloadHandler(ITimetableDbContext db, ICurrentTenant currentTenant)
    {
        _db = db;
        _currentTenant = currentTenant;
    }

    public async Task<Result<Guid>> Handle(SetFacultyWorkloadCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _currentTenant.TenantId!.Value;

        var existing = await _db.FacultyWorkloads
            .FirstOrDefaultAsync(x =>
                x.TenantId == tenantId &&
                x.FacultyUserId == request.FacultyUserId &&
                x.SemesterId == request.SemesterId,
                cancellationToken);

        if (existing is not null)
        {
            existing.MaxHoursPerWeek = request.MaxHoursPerWeek;
            await _db.SaveChangesAsync(cancellationToken);
            return Result.Success(existing.Id);
        }

        var workload = new FacultyWorkload
        {
            TenantId = tenantId,
            FacultyUserId = request.FacultyUserId,
            SemesterId = request.SemesterId,
            MaxHoursPerWeek = request.MaxHoursPerWeek
        };

        await _db.FacultyWorkloads.AddAsync(workload, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success(workload.Id);
    }
}
