using ERP.Reporting.Domain;
using ERP.Reporting.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Reporting.Application.Queries;

public record GetReportDefinitionQuery(Guid TenantId, Guid Id) : IRequest<Result<ReportDefinitionDetailDto>>;

public record ReportDefinitionDetailDto(
    Guid Id, string Code, string Name, string? Description,
    ReportCategory Category, string SqlQuery, bool IsBuiltIn, bool IsActive,
    IReadOnlyList<ReportColumnDto> Columns,
    IReadOnlyList<ReportFilterDto> Filters);

public record ReportColumnDto(Guid Id, string ColumnName, string DisplayName, string DataType, bool IsVisible, int OrderIndex, string? Format);
public record ReportFilterDto(Guid Id, string FilterKey, string DisplayName, string FilterType, bool IsRequired, string? DefaultValue, string? Options);

public class GetReportDefinitionHandler : IRequestHandler<GetReportDefinitionQuery, Result<ReportDefinitionDetailDto>>
{
    private readonly IReportingDbContext _db;

    public GetReportDefinitionHandler(IReportingDbContext db) => _db = db;

    public async Task<Result<ReportDefinitionDetailDto>> Handle(GetReportDefinitionQuery request, CancellationToken cancellationToken)
    {
        var definition = await _db.ReportDefinitions
            .Include(x => x.Columns)
            .Include(x => x.Filters)
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.TenantId == request.TenantId && !x.IsDeleted, cancellationToken);

        if (definition is null)
            return Result<ReportDefinitionDetailDto>.Failure("Report definition not found.");

        var dto = new ReportDefinitionDetailDto(
            definition.Id, definition.Code, definition.Name, definition.Description,
            definition.Category, definition.SqlQuery, definition.IsBuiltIn, definition.IsActive,
            definition.Columns.OrderBy(c => c.OrderIndex).Select(c =>
                new ReportColumnDto(c.Id, c.ColumnName, c.DisplayName, c.DataType, c.IsVisible, c.OrderIndex, c.Format)).ToList(),
            definition.Filters.Select(f =>
                new ReportFilterDto(f.Id, f.FilterKey, f.DisplayName, f.FilterType, f.IsRequired, f.DefaultValue, f.Options)).ToList()
        );

        return Result<ReportDefinitionDetailDto>.Success(dto);
    }
}
