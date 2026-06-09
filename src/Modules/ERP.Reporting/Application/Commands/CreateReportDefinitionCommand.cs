using System.Text.Json;
using ERP.Reporting.Domain;
using ERP.Reporting.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;

namespace ERP.Reporting.Application.Commands;

public record CreateReportDefinitionCommand(
    Guid TenantId,
    string Name,
    string? Description,
    ReportCategory Category,
    string SqlQuery,
    ColumnDto[] Columns,
    FilterDto[] Filters,
    string[] DefaultColumns) : IRequest<Result<Guid>>;

public record ColumnDto(string ColumnName, string DisplayName, string DataType, bool IsVisible, int OrderIndex, string? Format);
public record FilterDto(string FilterKey, string DisplayName, string FilterType, bool IsRequired, string? DefaultValue, string? Options);

public class CreateReportDefinitionHandler : IRequestHandler<CreateReportDefinitionCommand, Result<Guid>>
{
    private readonly IReportingDbContext _db;

    private static readonly string[] _blockedKeywords =
        { "drop", "delete", "update", "insert", "exec", "xp_", "sp_" };

    public CreateReportDefinitionHandler(IReportingDbContext db) => _db = db;

    public async Task<Result<Guid>> Handle(CreateReportDefinitionCommand request, CancellationToken cancellationToken)
    {
        var lowerSql = request.SqlQuery.ToLowerInvariant();
        foreach (var keyword in _blockedKeywords)
        {
            if (lowerSql.Contains(keyword))
                return Result<Guid>.Failure($"SQL contains disallowed keyword: {keyword}");
        }

        var definition = new ReportDefinition
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            Code = $"RPT-CUSTOM-{Guid.NewGuid():N}"[..20],
            Name = request.Name,
            Description = request.Description,
            Category = request.Category,
            SqlQuery = request.SqlQuery,
            IsBuiltIn = false,
            IsActive = true,
            DefaultColumns = JsonSerializer.Serialize(request.DefaultColumns)
        };

        var columns = request.Columns.Select((c, i) => new ReportColumn
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            ReportId = definition.Id,
            ColumnName = c.ColumnName,
            DisplayName = c.DisplayName,
            DataType = c.DataType,
            IsVisible = c.IsVisible,
            OrderIndex = c.OrderIndex,
            Format = c.Format
        }).ToList();

        var filters = request.Filters.Select(f => new ReportFilter
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            ReportId = definition.Id,
            FilterKey = f.FilterKey,
            DisplayName = f.DisplayName,
            FilterType = f.FilterType,
            IsRequired = f.IsRequired,
            DefaultValue = f.DefaultValue,
            Options = f.Options
        }).ToList();

        _db.ReportDefinitions.Add(definition);
        _db.ReportColumns.AddRange(columns);
        _db.ReportFilters.AddRange(filters);
        await _db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(definition.Id);
    }
}
