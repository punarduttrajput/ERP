using System.Text.Json;
using ERP.Reporting.Application.Services;
using ERP.Reporting.Domain;
using ERP.Reporting.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Reporting.Application.Commands;

public record SeedPredefinedReportsCommand(Guid TenantId) : IRequest<Result<int>>;

public class SeedPredefinedReportsHandler : IRequestHandler<SeedPredefinedReportsCommand, Result<int>>
{
    private readonly IReportingDbContext _db;

    public SeedPredefinedReportsHandler(IReportingDbContext db) => _db = db;

    public async Task<Result<int>> Handle(SeedPredefinedReportsCommand request, CancellationToken cancellationToken)
    {
        // IgnoreQueryFilters: startup seeder has no HTTP context so ICurrentTenant
        // resolves to Guid.Empty; the explicit TenantId condition still scopes correctly.
        var existingCodes = (await _db.ReportDefinitions
            .IgnoreQueryFilters()
            .Where(x => x.TenantId == request.TenantId && !x.IsDeleted)
            .Select(x => x.Code)
            .ToListAsync(cancellationToken)).ToHashSet();

        var toInsert = PredefinedReportRegistry.GetAll()
            .Where(r => !existingCodes.Contains(r.Code))
            .ToList();

        foreach (var report in toInsert)
        {
            var definition = new ReportDefinition
            {
                Id = Guid.NewGuid(),
                TenantId = request.TenantId,
                Code = report.Code,
                Name = report.Name,
                Category = report.Category,
                SqlQuery = report.SqlQuery,
                IsBuiltIn = true,
                IsActive = true,
                DefaultColumns = JsonSerializer.Serialize(report.DefaultColumns)
            };

            var columns = report.DefaultColumns.Select((col, i) => new ReportColumn
            {
                Id = Guid.NewGuid(),
                TenantId = request.TenantId,
                ReportId = definition.Id,
                ColumnName = col,
                DisplayName = col,
                DataType = "string",
                IsVisible = true,
                OrderIndex = i
            }).ToList();

            var filters = report.Filters.Select(f => new ReportFilter
            {
                Id = Guid.NewGuid(),
                TenantId = request.TenantId,
                ReportId = definition.Id,
                FilterKey = f.Key,
                DisplayName = f.DisplayName,
                FilterType = f.Type,
                IsRequired = f.IsRequired,
                DefaultValue = f.DefaultValue
            }).ToList();

            _db.ReportDefinitions.Add(definition);
            _db.ReportColumns.AddRange(columns);
            _db.ReportFilters.AddRange(filters);
        }

        if (toInsert.Any())
            await _db.SaveChangesAsync(cancellationToken);

        return Result<int>.Success(toInsert.Count);
    }
}
