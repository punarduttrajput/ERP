using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Common;
using ERP.Timetable.Domain;
using ERP.Timetable.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Timetable.Application.Commands;

public record AssignSubjectToFacultyCommand(
    Guid FacultyUserId,
    Guid SubjectId,
    Guid SemesterId,
    Guid BatchId,
    int HoursPerWeek) : IRequest<Result<Guid>>;

public class AssignSubjectToFacultyHandler : IRequestHandler<AssignSubjectToFacultyCommand, Result<Guid>>
{
    private readonly ITimetableDbContext _db;
    private readonly ICurrentTenant _currentTenant;

    public AssignSubjectToFacultyHandler(ITimetableDbContext db, ICurrentTenant currentTenant)
    {
        _db = db;
        _currentTenant = currentTenant;
    }

    public async Task<Result<Guid>> Handle(AssignSubjectToFacultyCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _currentTenant.TenantId!.Value;

        var existing = await _db.FacultySubjectAssignments
            .FirstOrDefaultAsync(x =>
                x.TenantId == tenantId &&
                x.FacultyUserId == request.FacultyUserId &&
                x.SubjectId == request.SubjectId &&
                x.SemesterId == request.SemesterId &&
                x.BatchId == request.BatchId,
                cancellationToken);

        if (existing is not null)
        {
            existing.HoursPerWeek = request.HoursPerWeek;
            await _db.SaveChangesAsync(cancellationToken);
            return Result.Success(existing.Id);
        }

        var assignment = new FacultySubjectAssignment
        {
            TenantId = tenantId,
            FacultyUserId = request.FacultyUserId,
            SubjectId = request.SubjectId,
            SemesterId = request.SemesterId,
            BatchId = request.BatchId,
            HoursPerWeek = request.HoursPerWeek
        };

        await _db.FacultySubjectAssignments.AddAsync(assignment, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success(assignment.Id);
    }
}
