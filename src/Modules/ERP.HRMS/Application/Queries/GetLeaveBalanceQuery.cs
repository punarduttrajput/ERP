using ERP.HRMS.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.HRMS.Application.Queries;

public record GetLeaveBalanceQuery(Guid EmployeeId, Guid TenantId, int Year) : IRequest<Result<IReadOnlyList<LeaveBalanceDto>>>;

public record LeaveBalanceDto(
    Guid LeaveTypeId,
    string LeaveTypeName,
    decimal TotalDays,
    decimal UsedDays,
    decimal PendingDays,
    decimal AvailableDays
);

public class GetLeaveBalanceHandler : IRequestHandler<GetLeaveBalanceQuery, Result<IReadOnlyList<LeaveBalanceDto>>>
{
    private readonly IHrmsDbContext _db;

    public GetLeaveBalanceHandler(IHrmsDbContext db)
    {
        _db = db;
    }

    public async Task<Result<IReadOnlyList<LeaveBalanceDto>>> Handle(GetLeaveBalanceQuery request, CancellationToken cancellationToken)
    {
        var balances = await _db.LeaveBalances
            .Where(b => b.TenantId == request.TenantId && b.EmployeeId == request.EmployeeId && b.Year == request.Year)
            .ToListAsync(cancellationToken);

        var typeIds = balances.Select(b => b.LeaveTypeId).ToList();
        var types = await _db.LeaveTypes
            .Where(t => typeIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, t => t.Name, cancellationToken);

        var result = balances.Select(b => new LeaveBalanceDto(
            b.LeaveTypeId,
            types.GetValueOrDefault(b.LeaveTypeId, "Unknown"),
            b.TotalDays,
            b.UsedDays,
            b.PendingDays,
            b.AvailableDays
        )).ToList();

        return Result.Success<IReadOnlyList<LeaveBalanceDto>>(result);
    }
}
