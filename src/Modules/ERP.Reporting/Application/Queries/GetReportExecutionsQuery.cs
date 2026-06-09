using ERP.Reporting.Domain;
using ERP.Reporting.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Reporting.Application.Queries;

public record GetReportExecutionsQuery(Guid TenantId, Guid? ReportId, int Page, int PageSize)
    : IRequest<PagedResult<ReportExecutionDto>>;

public record ReportExecutionDto(
    Guid Id, Guid ReportId, Guid? ExecutedBy, DateTime ExecutedAt,
    int RowCount, long DurationMs, ExportFormat? ExportFormat, bool IsScheduled);

public class GetReportExecutionsHandler : IRequestHandler<GetReportExecutionsQuery, PagedResult<ReportExecutionDto>>
{
    private readonly IReportingDbContext _db;

    public GetReportExecutionsHandler(IReportingDbContext db) => _db = db;

    public async Task<PagedResult<ReportExecutionDto>> Handle(GetReportExecutionsQuery request, CancellationToken cancellationToken)
    {
        var query = _db.ReportExecutions
            .Where(x => x.TenantId == request.TenantId && !x.IsDeleted);

        if (request.ReportId.HasValue)
            query = query.Where(x => x.ReportId == request.ReportId);

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.ExecutedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new ReportExecutionDto(
                x.Id, x.ReportId, x.ExecutedBy, x.ExecutedAt,
                x.RowCount, x.DurationMs, x.ExportFormat, x.IsScheduled))
            .ToListAsync(cancellationToken);

        return new PagedResult<ReportExecutionDto>(items, total, request.Page, request.PageSize);
    }
}
