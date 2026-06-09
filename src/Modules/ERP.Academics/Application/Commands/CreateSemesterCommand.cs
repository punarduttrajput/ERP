using ERP.Academics.Domain;
using ERP.Academics.Infrastructure;
using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Academics.Application.Commands;

public record CreateSemesterCommand(
    Guid AcademicYearId,
    int Number,
    string Label,
    DateOnly StartDate,
    DateOnly EndDate,
    bool IsCurrent) : IRequest<Result<Guid>>;

public class CreateSemesterHandler : IRequestHandler<CreateSemesterCommand, Result<Guid>>
{
    private readonly IAcademicsDbContext _db;
    private readonly ICurrentTenant _currentTenant;

    public CreateSemesterHandler(IAcademicsDbContext db, ICurrentTenant currentTenant)
    {
        _db = db;
        _currentTenant = currentTenant;
    }

    public async Task<Result<Guid>> Handle(CreateSemesterCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _currentTenant.TenantId ?? Guid.Empty;

        var yearExists = await _db.AcademicYears
            .AnyAsync(x => x.Id == request.AcademicYearId && x.TenantId == tenantId && !x.IsDeleted, cancellationToken);

        if (!yearExists)
            return Result.Failure<Guid>("Academic year not found.");

        var duplicate = await _db.Semesters
            .AnyAsync(x => x.TenantId == tenantId && x.AcademicYearId == request.AcademicYearId
                        && x.Number == request.Number && !x.IsDeleted, cancellationToken);

        if (duplicate)
            return Result.Failure<Guid>($"Semester {request.Number} already exists for this academic year.");

        var semester = new Semester
        {
            TenantId = tenantId,
            AcademicYearId = request.AcademicYearId,
            Number = request.Number,
            Label = request.Label,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            IsCurrent = request.IsCurrent
        };

        _db.Semesters.Add(semester);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(semester.Id);
    }
}
