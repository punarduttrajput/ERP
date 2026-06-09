using ERP.Academics.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Academics.Application.Queries;

public record AcademicYearDto(Guid Id, string Label, DateOnly StartDate, DateOnly EndDate, bool IsCurrent);

public record SemesterDto(Guid Id, Guid AcademicYearId, int Number, string Label, DateOnly StartDate, DateOnly EndDate, bool IsCurrent);

public record BatchDto(Guid Id, Guid ProgramId, Guid AcademicYearId, string Name, int AdmissionYear, int CurrentSemester, bool IsActive);

public record GetAcademicYearsQuery(int Page = 1, int PageSize = 20) : IRequest<IReadOnlyList<AcademicYearDto>>;

public record GetSemestersQuery(Guid? AcademicYearId = null, bool? IsCurrent = null) : IRequest<IReadOnlyList<SemesterDto>>;

public record GetBatchesQuery(Guid? ProgramId = null, int Page = 1, int PageSize = 20) : IRequest<IReadOnlyList<BatchDto>>;

public class GetAcademicYearsHandler : IRequestHandler<GetAcademicYearsQuery, IReadOnlyList<AcademicYearDto>>
{
    private readonly IAcademicsDbContext _db;

    public GetAcademicYearsHandler(IAcademicsDbContext db) => _db = db;

    public async Task<IReadOnlyList<AcademicYearDto>> Handle(GetAcademicYearsQuery request, CancellationToken cancellationToken)
    {
        return await _db.AcademicYears
            .Where(x => !x.IsDeleted)
            .OrderByDescending(x => x.StartDate)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new AcademicYearDto(x.Id, x.Label, x.StartDate, x.EndDate, x.IsCurrent))
            .ToListAsync(cancellationToken);
    }
}

public class GetSemestersHandler : IRequestHandler<GetSemestersQuery, IReadOnlyList<SemesterDto>>
{
    private readonly IAcademicsDbContext _db;

    public GetSemestersHandler(IAcademicsDbContext db) => _db = db;

    public async Task<IReadOnlyList<SemesterDto>> Handle(GetSemestersQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Semesters.Where(x => !x.IsDeleted);

        if (request.AcademicYearId.HasValue)
            query = query.Where(x => x.AcademicYearId == request.AcademicYearId.Value);

        if (request.IsCurrent.HasValue)
            query = query.Where(x => x.IsCurrent == request.IsCurrent.Value);

        return await query
            .OrderBy(x => x.Number)
            .Select(x => new SemesterDto(x.Id, x.AcademicYearId, x.Number, x.Label, x.StartDate, x.EndDate, x.IsCurrent))
            .ToListAsync(cancellationToken);
    }
}

public class GetBatchesHandler : IRequestHandler<GetBatchesQuery, IReadOnlyList<BatchDto>>
{
    private readonly IAcademicsDbContext _db;

    public GetBatchesHandler(IAcademicsDbContext db) => _db = db;

    public async Task<IReadOnlyList<BatchDto>> Handle(GetBatchesQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Batches.Where(x => !x.IsDeleted);

        if (request.ProgramId.HasValue)
            query = query.Where(x => x.ProgramId == request.ProgramId.Value);

        return await query
            .OrderByDescending(x => x.AdmissionYear)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new BatchDto(x.Id, x.ProgramId, x.AcademicYearId, x.Name, x.AdmissionYear, x.CurrentSemester, x.IsActive))
            .ToListAsync(cancellationToken);
    }
}
