using Dapper;
using ERP.Shared.Application.Abstractions;
using MediatR;

namespace ERP.MobileApi.Application.Queries;

public record SubjectAttendanceDto(string SubjectName, int Total, int Present, decimal Percent);

public record GetStudentAttendanceQuery(Guid TenantId, Guid UserId) : IRequest<IReadOnlyList<SubjectAttendanceDto>>;

public class GetStudentAttendanceHandler : IRequestHandler<GetStudentAttendanceQuery, IReadOnlyList<SubjectAttendanceDto>>
{
    private readonly IDbConnectionFactory _connectionFactory;

    public GetStudentAttendanceHandler(IDbConnectionFactory connectionFactory)
        => _connectionFactory = connectionFactory;

    public async Task<IReadOnlyList<SubjectAttendanceDto>> Handle(GetStudentAttendanceQuery request, CancellationToken cancellationToken)
    {
        using var conn = await _connectionFactory.CreateReadConnectionAsync(cancellationToken);

        const string studentSql = @"
            SELECT Id FROM students
            WHERE TenantId = @TenantId AND UserId = @UserId AND IsDeleted = 0 LIMIT 1";

        var studentId = await conn.QueryFirstOrDefaultAsync<Guid?>(studentSql, new
        {
            request.TenantId,
            request.UserId
        });

        if (studentId is null)
            return Array.Empty<SubjectAttendanceDto>();

        const string sql = @"
            SELECT subj.Name AS SubjectName,
                   COUNT(*) AS Total,
                   SUM(CASE WHEN ar.Status = 1 THEN 1 ELSE 0 END) AS Present
            FROM attendance_records ar
            JOIN attendance_sessions asn ON asn.Id = ar.SessionId
            JOIN subjects subj ON subj.Id = asn.SubjectId
            WHERE ar.TenantId = @TenantId AND ar.StudentId = @StudentId AND ar.IsDeleted = 0
            GROUP BY subj.Id, subj.Name
            ORDER BY subj.Name";

        var rows = await conn.QueryAsync<AttendanceSubjectRow>(sql, new
        {
            request.TenantId,
            StudentId = studentId.Value
        });

        return rows.Select(r =>
        {
            var pct = r.Total > 0 ? Math.Round((decimal)r.Present / r.Total * 100, 2) : 0m;
            return new SubjectAttendanceDto(r.SubjectName, r.Total, r.Present, pct);
        }).ToList();
    }

    private class AttendanceSubjectRow
    {
        public string SubjectName { get; set; } = string.Empty;
        public int Total { get; set; }
        public int Present { get; set; }
    }
}
