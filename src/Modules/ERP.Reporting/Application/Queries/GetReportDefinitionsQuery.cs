using ERP.Reporting.Domain;
using ERP.Reporting.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Reporting.Application.Queries;

public record GetReportDefinitionsQuery(
    Guid TenantId,
    ReportCategory? Category,
    bool? IsBuiltIn,
    int Page,
    int PageSize) : IRequest<PagedResult<ReportDefinitionSummaryDto>>;

public record ReportDefinitionSummaryDto(
    Guid Id, string Code, string Name, string? Description,
    ReportCategory Category, bool IsBuiltIn, bool IsActive);

public class GetReportDefinitionsHandler : IRequestHandler<GetReportDefinitionsQuery, PagedResult<ReportDefinitionSummaryDto>>
{
    private readonly IReportingDbContext _db;

    public GetReportDefinitionsHandler(IReportingDbContext db) => _db = db;

    public async Task<PagedResult<ReportDefinitionSummaryDto>> Handle(GetReportDefinitionsQuery request, CancellationToken cancellationToken)
    {
        var query = _db.ReportDefinitions
            .Where(x => x.TenantId == request.TenantId && !x.IsDeleted);

        if (request.Category.HasValue)
            query = query.Where(x => x.Category == request.Category);

        if (request.IsBuiltIn.HasValue)
            query = query.Where(x => x.IsBuiltIn == request.IsBuiltIn);

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(x => x.Category)
            .ThenBy(x => x.Name)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new ReportDefinitionSummaryDto(
                x.Id, x.Code, x.Name, x.Description, x.Category, x.IsBuiltIn, x.IsActive))
            .ToListAsync(cancellationToken);

        return new PagedResult<ReportDefinitionSummaryDto>(items, total, request.Page, request.PageSize);
    }
}
