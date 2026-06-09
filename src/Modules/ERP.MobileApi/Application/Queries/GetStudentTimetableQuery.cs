using Dapper;
using ERP.Shared.Application.Abstractions;
using MediatR;

namespace ERP.MobileApi.Application.Queries;

public record GetStudentTimetableQuery(Guid TenantId, Guid UserId, Guid SemesterId, Guid BatchId)
    : IRequest<TodayTimetableDto>;

public class GetStudentTimetableHandler : IRequestHandler<GetStudentTimetableQuery, TodayTimetableDto>
{
    private readonly IDbConnectionFactory _connectionFactory;

    public GetStudentTimetableHandler(IDbConnectionFactory connectionFactory)
        => _connectionFactory = connectionFactory;

    public async Task<TodayTimetableDto> Handle(GetStudentTimetableQuery request, CancellationToken cancellationToken)
    {
        using var conn = await _connectionFactory.CreateReadConnectionAsync(cancellationToken);

        const string sql = @"
            SELECT ts.PeriodNumber AS Period, subj.Name AS SubjectName,
                   ts.StartTime, ts.EndTime, ts.DayOfWeek,
                   r.Code AS RoomCode
            FROM timetable_entries te
            JOIN time_slots ts ON ts.Id = te.TimeSlotId
            JOIN subjects subj ON subj.Id = te.SubjectId
            LEFT JOIN rooms r ON r.Id = te.RoomId
            WHERE te.TenantId = @TenantId AND te.BatchId = @BatchId
              AND te.SemesterId = @SemesterId
              AND te.Status = 1 AND te.IsDeleted = 0
            ORDER BY ts.DayOfWeek, ts.PeriodNumber";

        var rows = await conn.QueryAsync<TimetableRow>(sql, new
        {
            request.TenantId,
            request.BatchId,
            request.SemesterId
        });

        var classes = rows.Select(r => new MobileClassDto(
            r.Period,
            r.SubjectName,
            r.StartTime.ToString(@"hh\:mm"),
            r.EndTime.ToString(@"hh\:mm"),
            r.RoomCode
        )).ToList();

        return new TodayTimetableDto(classes);
    }

    private class TimetableRow
    {
        public int Period { get; set; }
        public string SubjectName { get; set; } = string.Empty;
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public int DayOfWeek { get; set; }
        public string? RoomCode { get; set; }
    }
}
