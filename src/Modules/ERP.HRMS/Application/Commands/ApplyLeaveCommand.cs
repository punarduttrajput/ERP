using ERP.HRMS.Domain;
using ERP.HRMS.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.HRMS.Application.Commands;

public record ApplyLeaveCommand(
    Guid TenantId,
    Guid EmployeeId,
    Guid LeaveTypeId,
    DateOnly FromDate,
    DateOnly ToDate,
    string Reason
) : IRequest<Result<Guid>>;

public class ApplyLeaveHandler : IRequestHandler<ApplyLeaveCommand, Result<Guid>>
{
    private readonly IHrmsDbContext _db;

    public ApplyLeaveHandler(IHrmsDbContext db)
    {
        _db = db;
    }

    public async Task<Result<Guid>> Handle(ApplyLeaveCommand request, CancellationToken cancellationToken)
    {
        var balance = await _db.LeaveBalances
            .FirstOrDefaultAsync(b =>
                b.TenantId == request.TenantId &&
                b.EmployeeId == request.EmployeeId &&
                b.LeaveTypeId == request.LeaveTypeId &&
                b.Year == request.FromDate.Year, cancellationToken);

        if (balance is null)
            return Result.Failure<Guid>("Leave balance not found for this leave type.");

        var totalDays = CountWorkingDays(request.FromDate, request.ToDate);

        if (balance.AvailableDays < totalDays)
            return Result.Failure<Guid>($"Insufficient leave balance. Available: {balance.AvailableDays}, Requested: {totalDays}.");

        balance.PendingDays += totalDays;

        var app = new LeaveApplication
        {
            TenantId = request.TenantId,
            EmployeeId = request.EmployeeId,
            LeaveTypeId = request.LeaveTypeId,
            FromDate = request.FromDate,
            ToDate = request.ToDate,
            TotalDays = totalDays,
            Reason = request.Reason,
            Status = LeaveStatus.Pending
        };

        _db.LeaveApplications.Add(app);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(app.Id);
    }

    // Monday–Saturday are working days; Sunday is non-working
    private static decimal CountWorkingDays(DateOnly from, DateOnly to)
    {
        var count = 0;
        var current = from;
        while (current <= to)
        {
            if (current.DayOfWeek != DayOfWeek.Sunday)
                count++;
            current = current.AddDays(1);
        }
        return count;
    }
}
