using Dapper;
using ERP.MobileApi.Infrastructure;
using ERP.Shared.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.MobileApi.Application.Queries;

public record AnnouncementDto(string Title, string SubjectName, DateTime PostedAt);

public record FacultyDashboardDto(
    int PendingAttendanceSessions,
    int UngradedSubmissions,
    int TodayClasses,
    IReadOnlyList<AnnouncementDto> RecentAnnouncements,
    IReadOnlyList<NotificationDto> Notifications
);

public record GetFacultyDashboardQuery(Guid TenantId, Guid FacultyUserId, Guid SemesterId)
    : IRequest<FacultyDashboardDto>;

public class GetFacultyDashboardHandler : IRequestHandler<GetFacultyDashboardQuery, FacultyDashboardDto>
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IMobileDbContext _db;

    public GetFacultyDashboardHandler(IDbConnectionFactory connectionFactory, IMobileDbContext db)
    {
        _connectionFactory = connectionFactory;
        _db = db;
    }

    public async Task<FacultyDashboardDto> Handle(GetFacultyDashboardQuery request, CancellationToken cancellationToken)
    {
        using var conn = await _connectionFactory.CreateReadConnectionAsync(cancellationToken);

        // Sessions where faculty is assigned but attendance has not been marked yet
        const string pendingSql = @"
            SELECT COUNT(*) FROM attendance_sessions asn
            WHERE asn.TenantId = @TenantId AND asn.FacultyUserId = @FacultyUserId
              AND asn.SemesterId = @SemesterId AND asn.IsCompleted = 0 AND asn.IsDeleted = 0
              AND asn.ScheduledDate < NOW()";

        var pendingAttendance = await conn.QueryFirstOrDefaultAsync<int>(pendingSql, new
        {
            request.TenantId,
            request.FacultyUserId,
            request.SemesterId
        });

        // Assignment submissions in Submitted (1) status waiting for grading
        const string ungradedSql = @"
            SELECT COUNT(*) FROM assignment_submissions asub
            JOIN assignments a ON a.Id = asub.AssignmentId
            WHERE a.TenantId = @TenantId AND a.FacultyUserId = @FacultyUserId
              AND asub.Status = 1 AND asub.IsDeleted = 0 AND a.IsDeleted = 0";

        var ungradedSubmissions = await conn.QueryFirstOrDefaultAsync<int>(ungradedSql, new
        {
            request.TenantId,
            request.FacultyUserId
        });

        var dayOfWeek = (int)DateTime.UtcNow.DayOfWeek;
        const string todayClassesSql = @"
            SELECT COUNT(*) FROM timetable_entries te
            JOIN time_slots ts ON ts.Id = te.TimeSlotId
            WHERE te.TenantId = @TenantId AND te.FacultyUserId = @FacultyUserId
              AND te.SemesterId = @SemesterId AND ts.DayOfWeek = @DayOfWeek
              AND te.Status = 1 AND te.IsDeleted = 0";

        var todayClasses = await conn.QueryFirstOrDefaultAsync<int>(todayClassesSql, new
        {
            request.TenantId,
            request.FacultyUserId,
            request.SemesterId,
            DayOfWeek = dayOfWeek
        });

        const string announcementsSql = @"
            SELECT a.Title, subj.Name AS SubjectName, a.CreatedAt AS PostedAt
            FROM announcements a
            JOIN subjects subj ON subj.Id = a.SubjectId
            WHERE a.TenantId = @TenantId AND a.FacultyUserId = @FacultyUserId AND a.IsDeleted = 0
            ORDER BY a.CreatedAt DESC LIMIT 5";

        var announcementRows = await conn.QueryAsync<AnnouncementRow>(announcementsSql, new
        {
            request.TenantId,
            request.FacultyUserId
        });

        var announcements = announcementRows
            .Select(r => new AnnouncementDto(r.Title, r.SubjectName, r.PostedAt))
            .ToList();

        var notifications = await _db.PushNotifications
            .Where(n => n.TenantId == request.TenantId
                && n.RecipientUserId == request.FacultyUserId
                && !n.IsDeleted)
            .OrderByDescending(n => n.SentAt)
            .Take(5)
            .Select(n => new NotificationDto(
                n.Id, n.Title, n.Body,
                n.SentAt ?? n.CreatedAt,
                n.Status == Domain.NotificationStatus.Read))
            .ToListAsync(cancellationToken);

        return new FacultyDashboardDto(
            pendingAttendance,
            ungradedSubmissions,
            todayClasses,
            announcements,
            notifications
        );
    }

    private class AnnouncementRow
    {
        public string Title { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public DateTime PostedAt { get; set; }
    }
}
