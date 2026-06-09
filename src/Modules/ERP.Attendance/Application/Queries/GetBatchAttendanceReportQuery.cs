using Dapper;
using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Common;
using MediatR;

namespace ERP.Attendance.Application.Queries;

public record BatchAttendanceSessionRow(
    Guid SessionId,
    DateOnly SessionDate,
    int PeriodNumber,
    Guid SubjectId,
    Guid StudentId,
    int Status,
    string MarkedBy);

public record GetBatchAttendanceReportQuery(
    Guid TenantId,
    Guid BatchId,
    Guid SemesterId) : IRequest<Result<IReadOnlyList<BatchAttendanceSessionRow>>>;

public class GetBatchAttendanceReportHandler
    : IRequestHandler<GetBatchAttendanceReportQuery, Result<IReadOnlyList<BatchAttendanceSessionRow>>>
{
    private readonly IDbConnectionFactory _connectionFactory;

    public GetBatchAttendanceReportHandler(IDbConnectionFactory connectionFactory)
        => _connectionFactory = connectionFactory;

    public async Task<Result<IReadOnlyList<BatchAttendanceSessionRow>>> Handle(
        GetBatchAttendanceReportQuery request, CancellationToken cancellationToken)
    {
        using var conn = await _connectionFactory.CreateReadConnectionAsync(cancellationToken);

        const string sql = """
            SELECT
                s.Id AS SessionId,
                s.SessionDate,
                s.PeriodNumber,
                s.SubjectId,
                ar.StudentId,
                ar.Status,
                ar.MarkedBy
            FROM attendance_records ar
            JOIN attendance_sessions s ON ar.SessionId = s.Id
            WHERE s.TenantId = @TenantId
              AND s.BatchId = @BatchId
              AND s.SemesterId = @SemesterId
              AND ar.IsDeleted = 0
              AND s.IsDeleted = 0
            ORDER BY s.SessionDate, s.PeriodNumber, ar.StudentId
            """;

        var rows = await conn.QueryAsync(sql, new
        {
            request.TenantId,
            request.BatchId,
            request.SemesterId
        });

        var result = rows.Select(r => new BatchAttendanceSessionRow(
            (Guid)r.SessionId,
            DateOnly.FromDateTime((DateTime)r.SessionDate),
            (int)r.PeriodNumber,
            (Guid)r.SubjectId,
            (Guid)r.StudentId,
            (int)r.Status,
            (string)r.MarkedBy)).ToList();

        return Result<IReadOnlyList<BatchAttendanceSessionRow>>.Success(result);
    }
}
