using System.Text.Json;
using ERP.Reporting.Domain;
using ERP.Reporting.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Reporting.Application.Commands;

public record UpdateReportDefinitionCommand(
    Guid Id,
    Guid TenantId,
    string Name,
    string? Description,
    ReportCategory Category,
    string SqlQuery,
    string[] DefaultColumns,
    bool IsActive) : IRequest<Result>;

public class UpdateReportDefinitionHandler : IRequestHandler<UpdateReportDefinitionCommand, Result>
{
    private readonly IReportingDbContext _db;

    private static readonly string[] _blockedKeywords =
        { "drop", "delete", "update", "insert", "exec", "xp_", "sp_" };

    public UpdateReportDefinitionHandler(IReportingDbContext db) => _db = db;

    public async Task<Result> Handle(UpdateReportDefinitionCommand request, CancellationToken cancellationToken)
    {
        var definition = await _db.ReportDefinitions
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.TenantId == request.TenantId && !x.IsDeleted, cancellationToken);

        if (definition is null)
            return Result.Failure("Report definition not found.");

        if (definition.IsBuiltIn)
            return Result.Failure("Built-in reports cannot be modified.");

        var lowerSql = request.SqlQuery.ToLowerInvariant();
        foreach (var keyword in _blockedKeywords)
        {
            if (lowerSql.Contains(keyword))
                return Result.Failure($"SQL contains disallowed keyword: {keyword}");
        }

        definition.Name = request.Name;
        definition.Description = request.Description;
        definition.Category = request.Category;
        definition.SqlQuery = request.SqlQuery;
        definition.DefaultColumns = JsonSerializer.Serialize(request.DefaultColumns);
        definition.IsActive = request.IsActive;

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
