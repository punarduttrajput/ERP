using ERP.Academics.Domain;
using ERP.Academics.Infrastructure;
using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Academics.Application.Commands;

public record MapCurriculumCommand(
    Guid ProgramId,
    int SemesterNumber,
    Guid SubjectId,
    bool IsElective) : IRequest<Result<Guid>>;

public class MapCurriculumHandler : IRequestHandler<MapCurriculumCommand, Result<Guid>>
{
    private readonly IAcademicsDbContext _db;
    private readonly ICurrentTenant _currentTenant;

    public MapCurriculumHandler(IAcademicsDbContext db, ICurrentTenant currentTenant)
    {
        _db = db;
        _currentTenant = currentTenant;
    }

    public async Task<Result<Guid>> Handle(MapCurriculumCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _currentTenant.TenantId ?? Guid.Empty;

        var programExists = await _db.AcademicPrograms
            .AnyAsync(x => x.Id == request.ProgramId && x.TenantId == tenantId && !x.IsDeleted, cancellationToken);

        if (!programExists)
            return Result.Failure<Guid>("Program not found.");

        var subjectExists = await _db.Subjects
            .AnyAsync(x => x.Id == request.SubjectId && x.TenantId == tenantId && !x.IsDeleted, cancellationToken);

        if (!subjectExists)
            return Result.Failure<Guid>("Subject not found.");

        var duplicate = await _db.CurriculumEntries
            .AnyAsync(x => x.TenantId == tenantId
                        && x.ProgramId == request.ProgramId
                        && x.SemesterNumber == request.SemesterNumber
                        && x.SubjectId == request.SubjectId
                        && !x.IsDeleted, cancellationToken);

        if (duplicate)
            return Result.Failure<Guid>("This subject is already mapped to this program and semester.");

        var entry = new CurriculumEntry
        {
            TenantId = tenantId,
            ProgramId = request.ProgramId,
            SemesterNumber = request.SemesterNumber,
            SubjectId = request.SubjectId,
            IsElective = request.IsElective
        };

        _db.CurriculumEntries.Add(entry);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(entry.Id);
    }
}
