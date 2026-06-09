using Dapper;
using ERP.MobileApi.Infrastructure;
using ERP.Shared.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.MobileApi.Application.Queries;

public record TodayTimetableDto(IReadOnlyList<MobileClassDto> Classes);
public record MobileClassDto(int Period, string Subject, string StartTime, string EndTime, string? Room);
public record AttendanceSummaryMobileDto(int TotalClasses, int Present, decimal Percent);
public record LatestResultDto(decimal? Gpa, decimal? Cgpa, string SemesterLabel);
public record FeeStatusDto(decimal DueAmount, bool IsFullyPaid);
public record NotificationDto(Guid Id, string Title, string Body, DateTime SentAt, bool IsRead);

public record StudentDashboardDto(
    TodayTimetableDto Timetable,
    AttendanceSummaryMobileDto Attendance,
    LatestResultDto? LatestResult,
    FeeStatusDto Fees,
    IReadOnlyList<NotificationDto> RecentNotifications
);

public record GetStudentDashboardQuery(Guid TenantId, Guid UserId, DateTime Date)
    : IRequest<StudentDashboardDto>;

public class GetStudentDashboardHandler : IRequestHandler<GetStudentDashboardQuery, StudentDashboardDto>
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IMobileDbContext _db;

    public GetStudentDashboardHandler(IDbConnectionFactory connectionFactory, IMobileDbContext db)
    {
        _connectionFactory = connectionFactory;
        _db = db;
    }

    public async Task<StudentDashboardDto> Handle(GetStudentDashboardQuery request, CancellationToken cancellationToken)
    {
        using var conn = await _connectionFactory.CreateReadConnectionAsync(cancellationToken);

        // Resolve student record for the authenticated user — UserId maps to students.UserId
        const string studentSql = @"
            SELECT Id AS StudentId, BatchId, CurrentSemesterId AS SemesterId, EnrollmentNumber
            FROM students
            WHERE TenantId = @TenantId AND UserId = @UserId AND IsDeleted = 0
            LIMIT 1";

        var studentRow = await conn.QueryFirstOrDefaultAsync<StudentInfoRow>(studentSql, new
        {
            request.TenantId,
            request.UserId
        });

        if (studentRow is null)
            return EmptyDashboard();

        var dayOfWeek = (int)request.Date.DayOfWeek;

        const string timetableSql = @"
            SELECT ts.PeriodNumber AS Period, subj.Name AS SubjectName,
                   ts.StartTime, ts.EndTime, r.Code AS RoomCode
            FROM timetable_entries te
            JOIN time_slots ts ON ts.Id = te.TimeSlotId
            JOIN subjects subj ON subj.Id = te.SubjectId
            LEFT JOIN rooms r ON r.Id = te.RoomId
            WHERE te.TenantId = @TenantId AND te.BatchId = @BatchId
              AND te.SemesterId = @SemesterId AND ts.DayOfWeek = @DayOfWeek
              AND te.Status = 1 AND te.IsDeleted = 0
            ORDER BY ts.PeriodNumber";

        var classRows = await conn.QueryAsync<TimetableRow>(timetableSql, new
        {
            request.TenantId,
            studentRow.BatchId,
            studentRow.SemesterId,
            DayOfWeek = dayOfWeek
        });

        var classes = classRows.Select(r => new MobileClassDto(
            r.Period,
            r.SubjectName,
            r.StartTime.ToString(@"hh\:mm"),
            r.EndTime.ToString(@"hh\:mm"),
            r.RoomCode
        )).ToList();

        const string attendanceSql = @"
            SELECT COUNT(*) AS Total,
                   SUM(CASE WHEN ar.Status = 1 THEN 1 ELSE 0 END) AS Present
            FROM attendance_records ar
            WHERE ar.TenantId = @TenantId AND ar.StudentId = @StudentId AND ar.IsDeleted = 0";

        var attRow = await conn.QueryFirstOrDefaultAsync<AttendanceRow>(attendanceSql, new
        {
            request.TenantId,
            studentRow.StudentId
        });

        var totalClasses = attRow?.Total ?? 0;
        var present = attRow?.Present ?? 0;
        var percent = totalClasses > 0 ? Math.Round((decimal)present / totalClasses * 100, 2) : 0m;
        var attendance = new AttendanceSummaryMobileDto(totalClasses, present, percent);

        const string resultSql = @"
            SELECT GPA, CGPA, SemesterId FROM student_results
            WHERE TenantId = @TenantId AND StudentId = @StudentId AND IsPublished = 1 AND IsDeleted = 0
            ORDER BY CreatedAt DESC LIMIT 1";

        var resultRow = await conn.QueryFirstOrDefaultAsync<ResultRow>(resultSql, new
        {
            request.TenantId,
            studentRow.StudentId
        });

        LatestResultDto? latestResult = resultRow is not null
            ? new LatestResultDto(resultRow.GPA, resultRow.CGPA, resultRow.SemesterId.ToString())
            : null;

        const string feeSql = @"
            SELECT DueAmount, IsFullyPaid FROM student_fee_accounts
            WHERE TenantId = @TenantId AND StudentId = @StudentId AND IsDeleted = 0
            ORDER BY AcademicYear DESC LIMIT 1";

        var feeRow = await conn.QueryFirstOrDefaultAsync<FeeRow>(feeSql, new
        {
            request.TenantId,
            studentRow.StudentId
        });

        var fees = feeRow is not null
            ? new FeeStatusDto(feeRow.DueAmount, feeRow.IsFullyPaid)
            : new FeeStatusDto(0m, true);

        var recentNotifications = await _db.PushNotifications
            .Where(n => n.TenantId == request.TenantId
                && n.RecipientUserId == request.UserId
                && !n.IsDeleted)
            .OrderByDescending(n => n.SentAt)
            .Take(5)
            .Select(n => new NotificationDto(
                n.Id, n.Title, n.Body,
                n.SentAt ?? n.CreatedAt,
                n.Status == Domain.NotificationStatus.Read))
            .ToListAsync(cancellationToken);

        return new StudentDashboardDto(
            new TodayTimetableDto(classes),
            attendance,
            latestResult,
            fees,
            recentNotifications
        );
    }

    private static StudentDashboardDto EmptyDashboard() =>
        new(
            new TodayTimetableDto(Array.Empty<MobileClassDto>()),
            new AttendanceSummaryMobileDto(0, 0, 0m),
            null,
            new FeeStatusDto(0m, true),
            Array.Empty<NotificationDto>()
        );

    private class StudentInfoRow
    {
        public Guid StudentId { get; set; }
        public Guid BatchId { get; set; }
        public Guid SemesterId { get; set; }
        public string EnrollmentNumber { get; set; } = string.Empty;
    }

    private class TimetableRow
    {
        public int Period { get; set; }
        public string SubjectName { get; set; } = string.Empty;
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string? RoomCode { get; set; }
    }

    private class AttendanceRow
    {
        public int Total { get; set; }
        public int Present { get; set; }
    }

    private class ResultRow
    {
        public decimal? GPA { get; set; }
        public decimal? CGPA { get; set; }
        public Guid SemesterId { get; set; }
    }

    private class FeeRow
    {
        public decimal DueAmount { get; set; }
        public bool IsFullyPaid { get; set; }
    }
}
