using ERP.Admissions.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Admissions.Application.Queries;

public sealed class GetApplicationsHandler
    : IRequestHandler<GetApplicationsQuery, Result<PagedResult<ApplicationSummaryDto>>>
{
    private readonly IAdmissionsDbContext _db;

    public GetApplicationsHandler(IAdmissionsDbContext db) => _db = db;

    public async Task<Result<PagedResult<ApplicationSummaryDto>>> Handle(
        GetApplicationsQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Applications.AsNoTracking().AsQueryable();

        if (request.ProgramId.HasValue)
            query = query.Where(a => a.ProgramId == request.ProgramId.Value);
        if (request.AcademicYear.HasValue)
            query = query.Where(a => a.AcademicYear == request.AcademicYear.Value);
        if (request.State.HasValue)
            query = query.Where(a => a.State == request.State.Value);

        var total = await query.CountAsync(cancellationToken);

        var page     = request.Page     < 1   ? 1   : request.Page;
        var pageSize = request.PageSize is < 1 or > 100 ? 20 : request.PageSize;

        var items = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new ApplicationSummaryDto(
                a.Id, a.ApplicantName, a.ApplicantEmail, a.ProgramName,
                a.Category, a.AcademicYear, a.State, a.MeritScore, a.MeritRank, a.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result<PagedResult<ApplicationSummaryDto>>.Success(
            new PagedResult<ApplicationSummaryDto>(items, total, page, pageSize));
    }
}
