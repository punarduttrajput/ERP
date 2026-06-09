using Dapper;
using ERP.MobileApi.Infrastructure;
using ERP.Shared.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.MobileApi.Application.Queries;

public record ParentDashboardDto(
    string ChildName,
    string ProgramName,
    decimal OverallAttendancePercent,
    decimal? Cgpa,
    decimal FeesDue,
    IReadOnlyList<NotificationDto> Notifications
);

public record GetParentDashboardQuery(Guid TenantId, Guid ParentUserId, Guid ChildStudentId)
    : IRequest<ParentDashboardDto>;

public class GetParentDashboardHandler : IRequestHandler<GetParentDashboardQuery, ParentDashboardDto>
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IMobileDbContext _db;

    public GetParentDashboardHandler(IDbConnectionFactory connectionFactory, IMobileDbContext db)
    {
        _connectionFactory = connectionFactory;
        _db = db;
    }

    public async Task<ParentDashboardDto> Handle(GetParentDashboardQuery request, CancellationToken cancellationToken)
    {
        using var conn = await _connectionFactory.CreateReadConnectionAsync(cancellationToken);

        const string studentSql = @"
            SELECT CONCAT(s.FirstName, ' ', s.LastName) AS ChildName, p.Name AS ProgramName
            FROM students s
            JOIN academic_programs p ON p.Id = s.ProgramId
            WHERE s.TenantId = @TenantId AND s.Id = @ChildStudentId AND s.IsDeleted = 0
            LIMIT 1";

        var studentRow = await conn.QueryFirstOrDefaultAsync<StudentRow>(studentSql, new
        {
            request.TenantId,
            request.ChildStudentId
        });

        if (studentRow is null)
            return new ParentDashboardDto(string.Empty, string.Empty, 0m, null, 0m, Array.Empty<NotificationDto>());

        const string attendanceSql = @"
            SELECT COUNT(*) AS Total,
                   SUM(CASE WHEN ar.Status = 1 THEN 1 ELSE 0 END) AS Present
            FROM attendance_records ar
            WHERE ar.TenantId = @TenantId AND ar.StudentId = @ChildStudentId AND ar.IsDeleted = 0";

        var attRow = await conn.QueryFirstOrDefaultAsync<AttendanceRow>(attendanceSql, new
        {
            request.TenantId,
            request.ChildStudentId
        });

        var total = attRow?.Total ?? 0;
        var present = attRow?.Present ?? 0;
        var attendancePct = total > 0 ? Math.Round((decimal)present / total * 100, 2) : 0m;

        const string resultSql = @"
            SELECT CGPA FROM student_results
            WHERE TenantId = @TenantId AND StudentId = @ChildStudentId AND IsPublished = 1 AND IsDeleted = 0
            ORDER BY CreatedAt DESC LIMIT 1";

        var cgpa = await conn.QueryFirstOrDefaultAsync<decimal?>(resultSql, new
        {
            request.TenantId,
            request.ChildStudentId
        });

        const string feeSql = @"
            SELECT DueAmount FROM student_fee_accounts
            WHERE TenantId = @TenantId AND StudentId = @ChildStudentId AND IsDeleted = 0
            ORDER BY AcademicYear DESC LIMIT 1";

        var feesDue = await conn.QueryFirstOrDefaultAsync<decimal>(feeSql, new
        {
            request.TenantId,
            request.ChildStudentId
        });

        var notifications = await _db.PushNotifications
            .Where(n => n.TenantId == request.TenantId
                && n.RecipientUserId == request.ParentUserId
                && !n.IsDeleted)
            .OrderByDescending(n => n.SentAt)
            .Take(5)
            .Select(n => new NotificationDto(
                n.Id, n.Title, n.Body,
                n.SentAt ?? n.CreatedAt,
                n.Status == Domain.NotificationStatus.Read))
            .ToListAsync(cancellationToken);

        return new ParentDashboardDto(
            studentRow.ChildName,
            studentRow.ProgramName,
            attendancePct,
            cgpa,
            feesDue,
            notifications
        );
    }

    private class StudentRow
    {
        public string ChildName { get; set; } = string.Empty;
        public string ProgramName { get; set; } = string.Empty;
    }

    private class AttendanceRow
    {
        public int Total { get; set; }
        public int Present { get; set; }
    }
}
