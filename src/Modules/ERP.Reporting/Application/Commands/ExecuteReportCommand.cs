using System.Diagnostics;
using System.Text.Json;
using Dapper;
using ERP.Reporting.Application.Services;
using ERP.Reporting.Domain;
using ERP.Reporting.Infrastructure;
using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Reporting.Application.Commands;

public record ExecuteReportCommand(
    Guid TenantId,
    Guid? ReportDefinitionId,
    string? ReportCode,
    string? FiltersJson,
    Guid? ExecutedBy,
    bool IsScheduled = false) : IRequest<Result<ReportResultDto>>;

public record ReportResultDto(
    string ReportName,
    IReadOnlyList<string> Columns,
    IReadOnlyList<IDictionary<string, object?>> Rows,
    int TotalRows);

public class ExecuteReportHandler : IRequestHandler<ExecuteReportCommand, Result<ReportResultDto>>
{
    private readonly IReportingDbContext _db;
    private readonly IDbConnectionFactory _connectionFactory;

    public ExecuteReportHandler(IReportingDbContext db, IDbConnectionFactory connectionFactory)
    {
        _db = db;
        _connectionFactory = connectionFactory;
    }

    public async Task<Result<ReportResultDto>> Handle(ExecuteReportCommand request, CancellationToken cancellationToken)
    {
        string reportName;
        string sqlQuery;
        IReadOnlyList<string> columns;

        var predefined = request.ReportCode is not null
            ? PredefinedReportRegistry.GetAll().FirstOrDefault(r => r.Code == request.ReportCode)
            : null;

        if (predefined is not null)
        {
            reportName = predefined.Name;
            sqlQuery = predefined.SqlQuery;
            columns = predefined.DefaultColumns;
        }
        else if (request.ReportDefinitionId.HasValue)
        {
            var definition = await _db.ReportDefinitions
                .Include(x => x.Columns)
                .FirstOrDefaultAsync(x => x.Id == request.ReportDefinitionId && x.TenantId == request.TenantId && !x.IsDeleted, cancellationToken);

            if (definition is null)
                return Result<ReportResultDto>.Failure("Report definition not found.");

            reportName = definition.Name;
            sqlQuery = definition.SqlQuery;
            columns = definition.Columns
                .Where(c => c.IsVisible)
                .OrderBy(c => c.OrderIndex)
                .Select(c => c.ColumnName)
                .ToList();

            if (!columns.Any() && definition.DefaultColumns is not null)
                columns = JsonSerializer.Deserialize<string[]>(definition.DefaultColumns) ?? Array.Empty<string>();
        }
        else
        {
            return Result<ReportResultDto>.Failure("Either ReportDefinitionId or ReportCode must be provided.");
        }

        var parameters = new DynamicParameters();
        parameters.Add("TenantId", request.TenantId);

        if (!string.IsNullOrWhiteSpace(request.FiltersJson))
        {
            var filters = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(request.FiltersJson);
            if (filters is not null)
            {
                foreach (var (key, value) in filters)
                {
                    object? paramValue = value.ValueKind switch
                    {
                        JsonValueKind.Number when value.TryGetInt64(out var l) => l,
                        JsonValueKind.Number => value.GetDouble(),
                        JsonValueKind.String => value.GetString(),
                        JsonValueKind.True => true,
                        JsonValueKind.False => false,
                        JsonValueKind.Null => null,
                        _ => value.GetRawText()
                    };
                    parameters.Add(key, paramValue);
                }
            }
        }

        var sw = Stopwatch.StartNew();
        List<IDictionary<string, object?>> rows;

        using (var conn = await _connectionFactory.CreateReadConnectionAsync(cancellationToken))
        {
            var results = await conn.QueryAsync<dynamic>(sqlQuery, parameters);
            rows = results
                .Select(r => (IDictionary<string, object?>)
                    ((IDictionary<string, object>)r).ToDictionary(k => k.Key, k => (object?)k.Value))
                .ToList();
        }

        sw.Stop();

        if (!columns.Any() && rows.Any())
            columns = rows[0].Keys.ToList();

        var execution = new ReportExecution
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            ReportId = request.ReportDefinitionId ?? Guid.Empty,
            ExecutedBy = request.ExecutedBy,
            ExecutedAt = DateTime.UtcNow,
            FiltersJson = request.FiltersJson,
            RowCount = rows.Count,
            DurationMs = sw.ElapsedMilliseconds,
            IsScheduled = request.IsScheduled
        };
        _db.ReportExecutions.Add(execution);
        await _db.SaveChangesAsync(cancellationToken);

        return Result<ReportResultDto>.Success(new ReportResultDto(reportName, columns, rows, rows.Count));
    }
}
