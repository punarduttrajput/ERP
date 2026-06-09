using Dapper;
using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Common;
using MediatR;

namespace ERP.Attendance.Application.Queries;

public record AttendanceSummaryDto(
    Guid SubjectId,
    string SubjectName,
    int TotalSessions,
    int PresentCount,
    int AbsentCount,
    int LateCount,
    decimal AttendancePercentage);

public record GetStudentAttendanceSummaryQuery(
    Guid TenantId,
    Guid StudentId,
    Guid SemesterId) : IRequest<Result<IReadOnlyList<AttendanceSummaryDto>>>;

public class GetStudentAttendanceSummaryHandler
    : IRequestHandler<GetStudentAttendanceSummaryQuery, Result<IReadOnlyList<AttendanceSummaryDto>>>
{
    private readonly IDbConnectionFactory _connectionFactory;

    public GetStudentAttendanceSummaryHandler(IDbConnectionFactory connectionFactory)
        => _connectionFactory = connectionFactory;

    public async Task<Result<IReadOnlyList<AttendanceSummaryDto>>> Handle(
        GetStudentAttendanceSummaryQuery request, CancellationToken cancellationToken)
    {
        using var conn = await _connectionFactory.CreateReadConnectionAsync(cancellationToken);

        const string sql = """
            SELECT
                s.SubjectId,
                sub.Name AS SubjectName,
                COUNT(*) AS TotalSessions,
                SUM(CASE WHEN ar.Status = 1 THEN 1 ELSE 0 END) AS PresentCount,
                SUM(CASE WHEN ar.Status = 2 THEN 1 ELSE 0 END) AS AbsentCount,
                SUM(CASE WHEN ar.Status = 3 THEN 1 ELSE 0 END) AS LateCount
            FROM attendance_records ar
            JOIN attendance_sessions s ON ar.SessionId = s.Id
            LEFT JOIN subjects sub ON s.SubjectId = sub.Id
            WHERE s.TenantId = @TenantId
              AND s.SemesterId = @SemesterId
              AND ar.StudentId = @StudentId
              AND ar.IsDeleted = 0
              AND s.IsDeleted = 0
            GROUP BY s.SubjectId, sub.Name
            """;

        var rows = await conn.QueryAsync(sql, new
        {
            request.TenantId,
            request.SemesterId,
            request.StudentId
        });

        var result = rows.Select(r =>
        {
            int total = (int)r.TotalSessions;
            int present = (int)r.PresentCount;
            int absent = (int)r.AbsentCount;
            int late = (int)r.LateCount;
            decimal pct = total > 0 ? Math.Round((decimal)(present + late) / total * 100, 2) : 0m;

            return new AttendanceSummaryDto(
                (Guid)r.SubjectId,
                (string)(r.SubjectName ?? string.Empty),
                total,
                present,
                absent,
                late,
                pct);
        }).ToList();

        return Result<IReadOnlyList<AttendanceSummaryDto>>.Success(result);
    }
}
