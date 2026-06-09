using Dapper;
using ERP.OBE.Domain;
using ERP.OBE.Infrastructure;
using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.OBE.Application.Commands;

public record ComputeDirectAttainmentCommand(
    Guid TenantId,
    Guid SubjectId,
    Guid SemesterId,
    int AcademicYear,
    decimal ThresholdPercent = 60m) : IRequest<Result<int>>;

public class ComputeDirectAttainmentHandler : IRequestHandler<ComputeDirectAttainmentCommand, Result<int>>
{
    private readonly IObeDbContext _db;
    private readonly IDbConnectionFactory _connectionFactory;

    public ComputeDirectAttainmentHandler(IObeDbContext db, IDbConnectionFactory connectionFactory)
    {
        _db = db;
        _connectionFactory = connectionFactory;
    }

    public async Task<Result<int>> Handle(ComputeDirectAttainmentCommand request, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT
                c.Code AS CourseOutcomeCode,
                COUNT(DISTINCT em.StudentId) AS TotalStudents,
                SUM(CASE WHEN (em.Marks / em.MaxMarks * 100) >= @ThresholdPercent THEN 1 ELSE 0 END) AS StudentsAttained
            FROM external_marks em
            JOIN exam_schedules es ON em.ExamScheduleId = es.Id
            JOIN course_outcomes c ON c.SubjectId = es.SubjectId
            WHERE es.SubjectId = @SubjectId
              AND es.SemesterId = @SemesterId
              AND em.IsAbsent = 0
              AND em.IsDeleted = 0
              AND es.IsDeleted = 0
              AND c.IsDeleted = 0
            GROUP BY c.Code";

        using var conn = await _connectionFactory.CreateReadConnectionAsync(cancellationToken);
        var rows = await conn.QueryAsync<CoAttainmentRow>(sql, new
        {
            request.SubjectId,
            request.SemesterId,
            request.ThresholdPercent
        });

        var results = rows.ToList();
        if (results.Count == 0)
            return Result<int>.Failure("No exam marks found for the given subject and semester.");

        var now = DateTime.UtcNow;
        int count = 0;

        foreach (var row in results)
        {
            var attainmentPercent = row.TotalStudents > 0
                ? (decimal)row.StudentsAttained * 100m / row.TotalStudents
                : 0m;

            var level = ComputeLevel(attainmentPercent);

            var existing = await _db.DirectAttainments.FirstOrDefaultAsync(
                x => x.TenantId == request.TenantId
                  && x.SubjectId == request.SubjectId
                  && x.CourseOutcomeCode == row.CourseOutcomeCode
                  && x.SemesterId == request.SemesterId,
                cancellationToken);

            if (existing is not null)
            {
                existing.TotalStudents = row.TotalStudents;
                existing.StudentsAttained = row.StudentsAttained;
                existing.AttainmentPercent = Math.Round(attainmentPercent, 2);
                existing.ThresholdPercent = request.ThresholdPercent;
                existing.Level = level;
                existing.ComputedAt = now;
            }
            else
            {
                _db.DirectAttainments.Add(new DirectAttainment
                {
                    TenantId = request.TenantId,
                    SubjectId = request.SubjectId,
                    CourseOutcomeCode = row.CourseOutcomeCode,
                    SemesterId = request.SemesterId,
                    AcademicYear = request.AcademicYear,
                    TotalStudents = row.TotalStudents,
                    StudentsAttained = row.StudentsAttained,
                    AttainmentPercent = Math.Round(attainmentPercent, 2),
                    ThresholdPercent = request.ThresholdPercent,
                    Level = level,
                    ComputedAt = now
                });
            }

            count++;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Result<int>.Success(count);
    }

    public static AttainmentLevel ComputeLevel(decimal percent) =>
        percent < 40m ? AttainmentLevel.NotMet :
        percent < 60m ? AttainmentLevel.PartiallyMet :
        percent < 75m ? AttainmentLevel.Met :
                        AttainmentLevel.Exceeded;

    private sealed record CoAttainmentRow(string CourseOutcomeCode, int TotalStudents, int StudentsAttained);
}
