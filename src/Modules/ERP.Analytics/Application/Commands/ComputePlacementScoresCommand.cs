using Dapper;
using ERP.Analytics.Domain;
using ERP.Analytics.Infrastructure;
using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Analytics.Application.Commands;

public record ComputePlacementScoresCommand(Guid TenantId, int AcademicYear)
    : IRequest<Result<int>>;

public class ComputePlacementScoresHandler : IRequestHandler<ComputePlacementScoresCommand, Result<int>>
{
    private readonly IAnalyticsDbContext _db;
    private readonly IDbConnectionFactory _connectionFactory;

    public ComputePlacementScoresHandler(IAnalyticsDbContext db, IDbConnectionFactory connectionFactory)
    {
        _db = db;
        _connectionFactory = connectionFactory;
    }

    public async Task<Result<int>> Handle(ComputePlacementScoresCommand request, CancellationToken cancellationToken)
    {
        using var conn = await _connectionFactory.CreateReadConnectionAsync(cancellationToken);

        const string sql = @"
            SELECT
                s.Id AS StudentId,
                CONCAT(s.FirstName, ' ', s.LastName) AS StudentName,
                s.ProgramName,
                COALESCE(MAX(sr.CGPA), 0) AS Cgpa,
                COUNT(DISTINCT arr.Id) AS ActiveBacklogs,
                COALESCE(att.AttendancePct, 100.0) AS AttendancePercent
            FROM students s
            LEFT JOIN student_results sr ON sr.StudentId = s.Id AND sr.IsPublished = 1 AND sr.IsDeleted = 0
            LEFT JOIN arrear_registrations arr ON arr.StudentId = s.Id AND arr.IsDeleted = 0 AND arr.IsApproved = 0
            LEFT JOIN (
                SELECT ar.StudentId,
                       SUM(CASE WHEN ar.Status = 1 THEN 1 ELSE 0 END) * 100.0 / NULLIF(COUNT(*), 0) AS AttendancePct
                FROM attendance_records ar WHERE ar.IsDeleted = 0
                GROUP BY ar.StudentId
            ) att ON att.StudentId = s.Id
            WHERE s.TenantId = @TenantId AND s.AcademicYear = @AcademicYear
              AND s.IsActive = 1 AND s.IsDeleted = 0
            GROUP BY s.Id, s.FirstName, s.LastName, s.ProgramName, att.AttendancePct";

        var rows = await conn.QueryAsync<PlacementRow>(sql, new { request.TenantId, request.AcademicYear });

        var now = DateTime.UtcNow;
        int total = 0;

        foreach (var row in rows)
        {
            var (score, probability) = ComputePlacementScore(row.Cgpa, row.ActiveBacklogs, row.AttendancePercent);

            var existing = await _db.PlacementScores
                .FirstOrDefaultAsync(x => x.TenantId == request.TenantId
                    && x.StudentId == row.StudentId
                    && x.AcademicYear == request.AcademicYear, cancellationToken);

            if (existing is null)
            {
                existing = new PlacementScore
                {
                    TenantId = request.TenantId,
                    StudentId = row.StudentId,
                    AcademicYear = request.AcademicYear
                };
                _db.PlacementScores.Add(existing);
            }

            existing.StudentName = row.StudentName;
            existing.ProgramName = row.ProgramName;
            existing.Cgpa = row.Cgpa;
            existing.ActiveBacklogs = row.ActiveBacklogs;
            existing.AttendancePercent = row.AttendancePercent;
            existing.PlacementScoreValue = score;
            existing.PlacementProbabilityPercent = probability;
            existing.ComputedAt = now;

            total++;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Result<int>.Success(total);
    }

    internal static (decimal Score, decimal ProbabilityPercent) ComputePlacementScore(
        decimal cgpa, int activeBacklogs, decimal attendancePercent)
    {
        var cgpaScore = cgpa / 10.0m * 40m;
        var backlogPenalty = Math.Min(40m, activeBacklogs * 10m);
        var attendanceBonus = attendancePercent / 100m * 20m;
        var score = Math.Clamp(cgpaScore - backlogPenalty + attendanceBonus, 0m, 100m);

        var probability = score switch
        {
            >= 70m => 85m,
            >= 55m => 65m,
            >= 40m => 45m,
            _ => 20m
        };

        return (score, probability);
    }

    private class PlacementRow
    {
        public Guid StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string ProgramName { get; set; } = string.Empty;
        public decimal Cgpa { get; set; }
        public int ActiveBacklogs { get; set; }
        public decimal AttendancePercent { get; set; }
    }
}
