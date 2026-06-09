using ERP.Academics.Infrastructure;
using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Academics.Application.Queries;

public record ProgramDto(Guid Id, Guid DepartmentId, string Code, string Name, int DurationYears, int TotalSemesters, string DegreeType, bool IsActive);

public record GetProgramsQuery(Guid? DepartmentId = null, int Page = 1, int PageSize = 20) : IRequest<PagedResult<ProgramDto>>;

public class GetProgramsHandler : IRequestHandler<GetProgramsQuery, PagedResult<ProgramDto>>
{
    private readonly IAcademicsDbContext _db;

    public GetProgramsHandler(IAcademicsDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<ProgramDto>> Handle(GetProgramsQuery request, CancellationToken cancellationToken)
    {
        var query = _db.AcademicPrograms
            .Where(x => !x.IsDeleted);

        if (request.DepartmentId.HasValue)
            query = query.Where(x => x.DepartmentId == request.DepartmentId.Value);

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(x => x.Name)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new ProgramDto(x.Id, x.DepartmentId, x.Code, x.Name, x.DurationYears, x.TotalSemesters, x.DegreeType, x.IsActive))
            .ToListAsync(cancellationToken);

        return new PagedResult<ProgramDto>(items, total, request.Page, request.PageSize);
    }
}
