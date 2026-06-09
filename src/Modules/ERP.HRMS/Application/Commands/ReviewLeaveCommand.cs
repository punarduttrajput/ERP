using ERP.HRMS.Domain;
using ERP.HRMS.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.HRMS.Application.Commands;

public record ReviewLeaveCommand(
    Guid ApplicationId,
    Guid TenantId,
    bool Approve,
    Guid ReviewedBy,
    string? RejectionReason
) : IRequest<Result>;

public class ReviewLeaveHandler : IRequestHandler<ReviewLeaveCommand, Result>
{
    private readonly IHrmsDbContext _db;

    public ReviewLeaveHandler(IHrmsDbContext db)
    {
        _db = db;
    }

    public async Task<Result> Handle(ReviewLeaveCommand request, CancellationToken cancellationToken)
    {
        var app = await _db.LeaveApplications
            .FirstOrDefaultAsync(a => a.Id == request.ApplicationId && a.TenantId == request.TenantId, cancellationToken);

        if (app is null)
            return Result.Failure("Leave application not found.");

        if (app.Status != LeaveStatus.Pending)
            return Result.Failure("Only pending leave applications can be reviewed.");

        var balance = await _db.LeaveBalances
            .FirstOrDefaultAsync(b =>
                b.TenantId == request.TenantId &&
                b.EmployeeId == app.EmployeeId &&
                b.LeaveTypeId == app.LeaveTypeId &&
                b.Year == app.FromDate.Year, cancellationToken);

        if (request.Approve)
        {
            app.Status = LeaveStatus.Approved;
            app.ApprovedBy = request.ReviewedBy;
            app.ApprovedAt = DateTime.UtcNow;

            if (balance is not null)
            {
                balance.PendingDays -= app.TotalDays;
                balance.UsedDays += app.TotalDays;
            }

            var employee = await _db.Employees
                .FirstOrDefaultAsync(e => e.Id == app.EmployeeId, cancellationToken);

            if (employee is not null && app.FromDate == DateOnly.FromDateTime(DateTime.UtcNow.Date))
                employee.Status = EmploymentStatus.OnLeave;
        }
        else
        {
            app.Status = LeaveStatus.Rejected;
            app.RejectionReason = request.RejectionReason;

            if (balance is not null)
                balance.PendingDays -= app.TotalDays;
        }

        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
