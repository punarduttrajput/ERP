using Dapper;
using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Common;
using MediatR;

namespace ERP.Finance.Application.Queries;

public record BudgetUtilizationLineDto(
    Guid AccountId,
    string AccountName,
    decimal AllocatedAmount,
    decimal SpentAmount,
    decimal RemainingAmount,
    decimal UtilizationPercent
);

public record BudgetUtilizationDto(
    Guid BudgetId,
    string DepartmentName,
    int AcademicYear,
    decimal TotalAllocated,
    decimal TotalSpent,
    decimal OverallUtilizationPercent,
    List<BudgetUtilizationLineDto> Lines
);

public record GetBudgetUtilizationQuery(Guid TenantId, Guid BudgetId) : IRequest<Result<BudgetUtilizationDto>>;

public class GetBudgetUtilizationHandler : IRequestHandler<GetBudgetUtilizationQuery, Result<BudgetUtilizationDto>>
{
    private readonly IDbConnectionFactory _connectionFactory;

    public GetBudgetUtilizationHandler(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Result<BudgetUtilizationDto>> Handle(GetBudgetUtilizationQuery request, CancellationToken cancellationToken)
    {
        using var conn = await _connectionFactory.CreateReadConnectionAsync(cancellationToken);

        const string headerSql = @"
SELECT Id AS BudgetId, DepartmentName, AcademicYear, TotalAllocated, TotalSpent
FROM budgets
WHERE Id = @BudgetId AND TenantId = @TenantId AND IsDeleted = 0
LIMIT 1";

        const string linesSql = @"
SELECT
    AccountId,
    AccountName,
    AllocatedAmount,
    SpentAmount,
    (AllocatedAmount - SpentAmount) AS RemainingAmount,
    CASE WHEN AllocatedAmount = 0 THEN 0 ELSE (SpentAmount / AllocatedAmount * 100) END AS UtilizationPercent
FROM budget_lines
WHERE BudgetId = @BudgetId AND TenantId = @TenantId AND IsDeleted = 0";

        var header = await conn.QueryFirstOrDefaultAsync<dynamic>(headerSql, new { request.BudgetId, TenantId = request.TenantId });
        if (header is null)
            return Result.Failure<BudgetUtilizationDto>("Budget not found.");

        var lines = (await conn.QueryAsync<BudgetUtilizationLineDto>(linesSql, new { request.BudgetId, TenantId = request.TenantId })).ToList();

        decimal totalAllocated = (decimal)header.TotalAllocated;
        decimal totalSpent = (decimal)header.TotalSpent;
        decimal utilization = totalAllocated == 0 ? 0 : totalSpent / totalAllocated * 100;

        return Result.Success(new BudgetUtilizationDto(
            request.BudgetId,
            (string)header.DepartmentName,
            (int)header.AcademicYear,
            totalAllocated,
            totalSpent,
            utilization,
            lines
        ));
    }
}
