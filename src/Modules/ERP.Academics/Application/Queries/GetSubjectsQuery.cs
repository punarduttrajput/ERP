using ERP.Academics.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Academics.Application.Queries;

public record CourseOutcomeDto(Guid Id, string Code, string Description);

public record SubjectDto(Guid Id, Guid ProgramId, string Code, string Name, int Credits, int ContactHoursPerWeek, string SubjectType, string? SyllabusUrl, bool IsActive, IReadOnlyList<CourseOutcomeDto> CourseOutcomes);

public record GetSubjectsQuery(Guid? ProgramId = null, int Page = 1, int PageSize = 20) : IRequest<PagedResult<SubjectDto>>;

public class GetSubjectsHandler : IRequestHandler<GetSubjectsQuery, PagedResult<SubjectDto>>
{
    private readonly IAcademicsDbContext _db;

    public GetSubjectsHandler(IAcademicsDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<SubjectDto>> Handle(GetSubjectsQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Subjects
            .Where(x => !x.IsDeleted);

        if (request.ProgramId.HasValue)
            query = query.Where(x => x.ProgramId == request.ProgramId.Value);

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(x => x.Code)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new SubjectDto(
                x.Id, x.ProgramId, x.Code, x.Name, x.Credits, x.ContactHoursPerWeek,
                x.SubjectType, x.SyllabusUrl, x.IsActive,
                x.CourseOutcomes.Where(co => !co.IsDeleted)
                    .Select(co => new CourseOutcomeDto(co.Id, co.Code, co.Description))
                    .ToList()))
            .ToListAsync(cancellationToken);

        return new PagedResult<SubjectDto>(items, total, request.Page, request.PageSize);
    }
}
