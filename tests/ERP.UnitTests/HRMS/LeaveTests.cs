using ERP.HRMS.Domain;
using FluentAssertions;
using Xunit;

namespace ERP.UnitTests.HRMS;

public class LeaveTests
{
    private static LeaveBalance CreateBalance(decimal total, decimal used = 0, decimal pending = 0)
    {
        return new LeaveBalance
        {
            TenantId = Guid.NewGuid(),
            EmployeeId = Guid.NewGuid(),
            LeaveTypeId = Guid.NewGuid(),
            Year = 2026,
            TotalDays = total,
            UsedDays = used,
            PendingDays = pending
        };
    }

    private static LeaveApplication CreateApplication(Guid employeeId, Guid leaveTypeId, decimal days)
    {
        return new LeaveApplication
        {
            TenantId = Guid.NewGuid(),
            EmployeeId = employeeId,
            LeaveTypeId = leaveTypeId,
            FromDate = new DateOnly(2026, 6, 1),
            ToDate = new DateOnly(2026, 6, 5),
            TotalDays = days,
            Reason = "Test leave",
            Status = LeaveStatus.Pending
        };
    }

    [Fact]
    public void ApplyLeave_DeductsPendingBalance()
    {
        var balance = CreateBalance(total: 12, used: 0, pending: 0);
        var days = 3m;

        // Simulate what ApplyLeaveHandler does
        balance.PendingDays += days;

        balance.PendingDays.Should().Be(3m);
        balance.AvailableDays.Should().Be(9m); // 12 - 0 - 3
    }

    [Fact]
    public void ApproveLeave_MovesFromPendingToUsed()
    {
        var balance = CreateBalance(total: 12, used: 0, pending: 3);
        var days = 3m;

        // Simulate what ReviewLeaveHandler does on approval
        balance.PendingDays -= days;
        balance.UsedDays += days;

        balance.PendingDays.Should().Be(0m);
        balance.UsedDays.Should().Be(3m);
        balance.AvailableDays.Should().Be(9m); // 12 - 3 - 0
    }

    [Fact]
    public void RejectLeave_RestoresPendingBalance()
    {
        var balance = CreateBalance(total: 12, used: 0, pending: 3);
        var days = 3m;

        // Simulate what ReviewLeaveHandler does on rejection
        balance.PendingDays -= days;

        balance.PendingDays.Should().Be(0m);
        balance.UsedDays.Should().Be(0m);
        balance.AvailableDays.Should().Be(12m); // fully restored
    }
}
