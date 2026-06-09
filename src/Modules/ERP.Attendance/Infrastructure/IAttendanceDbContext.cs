using ERP.Attendance.Domain;
using Microsoft.EntityFrameworkCore;

namespace ERP.Attendance.Infrastructure;

public interface IAttendanceDbContext
{
    DbSet<AttendanceSession> AttendanceSessions { get; }
    DbSet<AttendanceRecord> AttendanceRecords { get; }
    DbSet<RegularizationRequest> RegularizationRequests { get; }
    DbSet<BiometricLog> BiometricLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
