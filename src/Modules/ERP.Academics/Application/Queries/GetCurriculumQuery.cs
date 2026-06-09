using ERP.Academics.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Academics.Application.Queries;

public record CurriculumSubjectDto(Guid EntryId, Guid SubjectId, string SubjectCode, string SubjectName, int Credits, string SubjectType, bool IsElective);

public record GetCurriculumQuery(Guid ProgramId, int? SemesterNumber = null) : IRequest<IReadOnlyList<CurriculumSubjectDto>>;

public class GetCurriculumHandler : IRequestHandler<GetCurriculumQuery, IReadOnlyList<CurriculumSubjectDto>>
{
    private readonly IAcademicsDbContext _db;

    public GetCurriculumHandler(IAcademicsDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<CurriculumSubjectDto>> Handle(GetCurriculumQuery request, CancellationToken cancellationToken)
    {
        var query = _db.CurriculumEntries
            .Where(x => x.ProgramId == request.ProgramId && !x.IsDeleted);

        if (request.SemesterNumber.HasValue)
            query = query.Where(x => x.SemesterNumber == request.SemesterNumber.Value);

        return await query
            .OrderBy(x => x.SemesterNumber)
            .Select(x => new CurriculumSubjectDto(
                x.Id,
                x.SubjectId,
                x.Subject!.Code,
                x.Subject.Name,
                x.Subject.Credits,
                x.Subject.SubjectType,
                x.IsElective))
            .ToListAsync(cancellationToken);
    }
}
