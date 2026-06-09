using ERP.Attendance.Infrastructure;
using ERP.LMS.Domain;
using ERP.LMS.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ERP.Chatbot.Application.Services.IntentHandlers;

public sealed class FacultyBriefIntentHandler
{
    private readonly IAttendanceDbContext _attendanceDb;
    private readonly ILmsDbContext _lmsDb;

    public FacultyBriefIntentHandler(IAttendanceDbContext attendanceDb, ILmsDbContext lmsDb)
    {
        _attendanceDb = attendanceDb;
        _lmsDb = lmsDb;
    }

    public async Task<string> HandleAsync(Guid tenantId, Guid userId, string userMessage, CancellationToken ct)
    {
        var unmarkedCount = await _attendanceDb.AttendanceSessions
            .Where(s => s.FacultyUserId == userId && !s.IsLocked)
            .CountAsync(ct);

        var facultyAssignmentIds = await _lmsDb.Assignments
            .Where(a => a.AssignmentCreatedBy == userId)
            .Select(a => a.Id)
            .ToListAsync(ct);

        var ungradedCount = await _lmsDb.AssignmentSubmissions
            .Where(s => facultyAssignmentIds.Contains(s.AssignmentId) &&
                        (s.Status == SubmissionStatus.Submitted || s.Status == SubmissionStatus.Late))
            .CountAsync(ct);

        return $"Good morning! You have {unmarkedCount} unmarked attendance session(s) and {ungradedCount} ungraded assignment submission(s) pending your review.";
    }
}
