using ERP.Admissions.Domain;
using ERP.Admissions.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Admissions.Application.Queries;

public sealed class GetMeritListHandler
    : IRequestHandler<GetMeritListQuery, Result<IReadOnlyList<MeritListEntryDto>>>
{
    private readonly IAdmissionsDbContext _db;
    public GetMeritListHandler(IAdmissionsDbContext db) => _db = db;

    public async Task<Result<IReadOnlyList<MeritListEntryDto>>> Handle(
        GetMeritListQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Applications.AsNoTracking()
            .Where(a => a.ProgramId == request.ProgramId
                     && a.AcademicYear == request.AcademicYear
                     && a.MeritRank.HasValue);

        if (request.Category is not null)
            query = query.Where(a => a.Category == request.Category);

        var list = await query
            .OrderBy(a => a.MeritRank)
            .Select(a => new MeritListEntryDto(
                a.Id, a.MeritRank!.Value, a.ApplicantName, a.Category, a.MeritScore, a.State))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<MeritListEntryDto>>.Success(list);
    }
}
