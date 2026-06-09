using Dapper;
using ERP.Shared.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace ERP.Attendance.Application.Jobs;

public class AttendanceAlertJob
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ISmsService _smsService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<AttendanceAlertJob> _logger;

    public AttendanceAlertJob(
        IDbConnectionFactory connectionFactory,
        ISmsService smsService,
        ICacheService cacheService,
        ILogger<AttendanceAlertJob> logger)
    {
        _connectionFactory = connectionFactory;
        _smsService = smsService;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        using var conn = await _connectionFactory.CreateReadConnectionAsync(cancellationToken);

        // Fetch all active tenant+semester combos that have attendance data
        const string tenantSql = """
            SELECT DISTINCT s.TenantId, s.SemesterId
            FROM attendance_sessions s
            WHERE s.IsDeleted = 0
            """;

        var combos = (await conn.QueryAsync(tenantSql)).ToList();

        foreach (var combo in combos)
        {
            Guid tenantId = combo.TenantId;
            Guid semesterId = combo.SemesterId;

            const string below75Sql = """
                SELECT
                    ar.StudentId,
                    s.SubjectId,
                    COUNT(*) AS TotalSessions,
                    SUM(CASE WHEN ar.Status = 1 THEN 1 ELSE 0 END) AS PresentCount
                FROM attendance_records ar
                JOIN attendance_sessions s ON ar.SessionId = s.Id
                WHERE s.TenantId = @TenantId
                  AND s.SemesterId = @SemesterId
                  AND ar.IsDeleted = 0
                  AND s.IsDeleted = 0
                GROUP BY ar.StudentId, s.SubjectId
                HAVING (PresentCount * 1.0 / TotalSessions) < 0.75
                """;

            var belowThreshold = (await conn.QueryAsync(below75Sql, new { TenantId = tenantId, SemesterId = semesterId })).ToList();

            foreach (var row in belowThreshold)
            {
                Guid studentId = row.StudentId;
                Guid subjectId = row.SubjectId;

                // Deduplicate: only alert once every 7 days per student+subject combo
                var cacheKey = $"attn_alert:{tenantId}:{studentId}:{subjectId}";
                var alreadyAlerted = await _cacheService.ExistsAsync(cacheKey, cancellationToken);
                if (alreadyAlerted)
                    continue;

                // Fetch the student's mobile number
                const string mobileSql = """
                    SELECT sp.MobileNumber
                    FROM students st
                    JOIN user_profiles sp ON st.UserId = sp.UserId
                    WHERE st.Id = @StudentId AND st.TenantId = @TenantId AND st.IsDeleted = 0
                    LIMIT 1
                    """;

                var mobile = await conn.QueryFirstOrDefaultAsync<string>(mobileSql, new { StudentId = studentId, TenantId = tenantId });

                if (string.IsNullOrWhiteSpace(mobile))
                {
                    _logger.LogWarning("No mobile number found for student {StudentId}, skipping alert.", studentId);
                    continue;
                }

                int total = (int)row.TotalSessions;
                int present = (int)row.PresentCount;
                decimal pct = total > 0 ? Math.Round((decimal)present / total * 100, 1) : 0m;

                var message = $"Attendance Alert: Your attendance for subject {subjectId} has fallen to {pct}%, which is below the required 75%. Please contact your faculty.";

                try
                {
                    await _smsService.SendAsync(mobile, message, cancellationToken);
                    // TTL of 7 days prevents re-alerting the same student+subject within the week
                    await _cacheService.SetAsync(cacheKey, new AlertSentMarker(), TimeSpan.FromDays(7), cancellationToken);
                    _logger.LogInformation("Attendance alert sent to student {StudentId} for subject {SubjectId} ({Pct}%).", studentId, subjectId, pct);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send attendance alert to student {StudentId}.", studentId);
                }
            }
        }
    }

    private sealed class AlertSentMarker { }
}
