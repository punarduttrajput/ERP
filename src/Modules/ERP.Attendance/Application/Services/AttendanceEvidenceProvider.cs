using Dapper;
using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Contracts;

namespace ERP.Attendance.Application.Services;

public class AttendanceEvidenceProvider : IEvidenceProvider
{
    private readonly IDbConnectionFactory _connectionFactory;

    public AttendanceEvidenceProvider(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public string ModuleName => "Attendance";

    public async Task<IReadOnlyList<EvidenceItem>> GetEvidenceAsync(
        Guid tenantId,
        int academicYear,
        CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateReadConnectionAsync(cancellationToken);

        var rows = await connection.QueryAsync<AttendanceRow>(
            @"SELECT ar.StudentId, s.SubjectId,
                     COUNT(*) AS TotalSessions,
                     SUM(CASE WHEN ar.Status = 1 THEN 1 ELSE 0 END) AS PresentCount
              FROM attendance_records ar
              JOIN attendance_sessions s ON ar.SessionId = s.Id
              WHERE s.TenantId = @TenantId
                AND s.SemesterId IN (
                    SELECT Id FROM semesters
                    WHERE TenantId = @TenantId AND YEAR(StartDate) = @AcademicYear AND IsDeleted = 0
                )
                AND ar.IsDeleted = 0
                AND s.IsDeleted = 0
              GROUP BY ar.StudentId, s.SubjectId",
            new { TenantId = tenantId, AcademicYear = academicYear });

        return rows.Select(r => new EvidenceItem(
            Module: "Attendance",
            Category: "Attendance",
            Key: $"{r.StudentId}_{r.SubjectId}",
            Label: $"Student {r.StudentId} — Subject {r.SubjectId}",
            NumericValue: r.TotalSessions > 0
                ? Math.Round((decimal)r.PresentCount / r.TotalSessions * 100, 2)
                : 0m,
            TextValue: null,
            RecordedAt: DateTime.UtcNow,
            Metadata: new Dictionary<string, string>
            {
                { "totalSessions", r.TotalSessions.ToString() },
                { "presentCount", r.PresentCount.ToString() }
            }
        )).ToList();
    }

    private sealed class AttendanceRow
    {
        public Guid StudentId { get; init; }
        public Guid SubjectId { get; init; }
        public int TotalSessions { get; init; }
        public int PresentCount { get; init; }
    }
}
