using ERP.Attendance.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ERP.Chatbot.Application.Services.IntentHandlers;

public sealed class AttendanceSummaryIntentHandler
{
    private readonly IAttendanceDbContext _attendanceDb;

    public AttendanceSummaryIntentHandler(IAttendanceDbContext attendanceDb) => _attendanceDb = attendanceDb;

    public async Task<string> HandleAsync(Guid tenantId, Guid userId, string userMessage, CancellationToken ct)
    {
        var records = await _attendanceDb.AttendanceRecords
            .Include(r => r.Session)
            .Where(r => r.StudentId == userId && r.Session != null)
            .ToListAsync(ct);

        if (!records.Any())
            return "No attendance records found for your account.";

        var bySubject = records
            .GroupBy(r => r.Session!.SubjectId)
            .Select(g => new
            {
                SubjectId = g.Key,
                Total = g.Count(),
                Present = g.Count(r => r.Status == ERP.Attendance.Domain.AttendanceStatus.Present)
            })
            .ToList();

        var lines = bySubject.Select(s =>
        {
            var pct = s.Total > 0 ? (s.Present * 100.0 / s.Total) : 0;
            return $"{s.SubjectId.ToString()[..8]}: {pct:F1}%";
        });

        var overallPct = bySubject.Sum(s => s.Total) > 0
            ? bySubject.Sum(s => s.Present) * 100.0 / bySubject.Sum(s => s.Total)
            : 0;

        return $"Your attendance summary: {string.Join(", ", lines)}. Overall: {overallPct:F1}%.";
    }
}
