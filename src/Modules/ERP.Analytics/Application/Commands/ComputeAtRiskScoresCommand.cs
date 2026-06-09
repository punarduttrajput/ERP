using Dapper;
using ERP.Analytics.Domain;
using ERP.Analytics.Infrastructure;
using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Analytics.Application.Commands;

public record ComputeAtRiskResult(int TotalScored, int HighCriticalCount);

public record ComputeAtRiskScoresCommand(Guid TenantId, Guid SemesterId, int AcademicYear)
    : IRequest<Result<ComputeAtRiskResult>>;

public class ComputeAtRiskScoresHandler : IRequestHandler<ComputeAtRiskScoresCommand, Result<ComputeAtRiskResult>>
{
    private readonly IAnalyticsDbContext _db;
    private readonly IDbConnectionFactory _connectionFactory;

    public ComputeAtRiskScoresHandler(IAnalyticsDbContext db, IDbConnectionFactory connectionFactory)
    {
        _db = db;
        _connectionFactory = connectionFactory;
    }

    public async Task<Result<ComputeAtRiskResult>> Handle(ComputeAtRiskScoresCommand request, CancellationToken cancellationToken)
    {
        using var conn = await _connectionFactory.CreateReadConnectionAsync(cancellationToken);

        const string sql = @"
            SELECT
                s.Id AS StudentId,
                CONCAT(s.FirstName, ' ', s.LastName) AS StudentName,
                s.ProgramName,
                COALESCE(att.AttendancePercent, 100.0) AS AttendancePercent,
                COALESCE(res.AvgMarksPercent, 100.0) AS AverageMarksPercent
            FROM students s
            LEFT JOIN (
                SELECT ar.StudentId,
                       (SUM(CASE WHEN ar.Status = 1 THEN 1 ELSE 0 END) * 100.0 / NULLIF(COUNT(*), 0)) AS AttendancePercent
                FROM attendance_records ar
                JOIN attendance_sessions asn ON ar.SessionId = asn.Id
                WHERE asn.SemesterId = @SemesterId AND ar.IsDeleted = 0 AND asn.IsDeleted = 0
                GROUP BY ar.StudentId
            ) att ON att.StudentId = s.Id
            LEFT JOIN (
                SELECT StudentId, AVG(TotalMarks * 100.0 / NULLIF(MaxMarks, 0)) AS AvgMarksPercent
                FROM student_results
                WHERE SemesterId = @SemesterId AND IsPublished = 1 AND IsDeleted = 0
                GROUP BY StudentId
            ) res ON res.StudentId = s.Id
            WHERE s.TenantId = @TenantId AND s.IsActive = 1 AND s.IsDeleted = 0
              AND s.AcademicYear = @AcademicYear";

        var rows = await conn.QueryAsync<StudentRiskRow>(sql, new
        {
            request.TenantId,
            request.SemesterId,
            request.AcademicYear
        });

        var now = DateTime.UtcNow;
        int highCritical = 0;
        int total = 0;

        foreach (var row in rows)
        {
            var (score, level, attFlag, marksFlag) = ComputeAtRiskScore(row.AttendancePercent, row.AverageMarksPercent);

            var existing = await _db.StudentRiskScores
                .FirstOrDefaultAsync(x => x.TenantId == request.TenantId
                    && x.StudentId == row.StudentId
                    && x.SemesterId == request.SemesterId, cancellationToken);

            if (existing is null)
            {
                existing = new StudentRiskScore
                {
                    TenantId = request.TenantId,
                    StudentId = row.StudentId,
                    SemesterId = request.SemesterId
                };
                _db.StudentRiskScores.Add(existing);
            }

            existing.StudentName = row.StudentName;
            existing.ProgramName = row.ProgramName;
            existing.AcademicYear = request.AcademicYear;
            existing.AttendancePercent = row.AttendancePercent;
            existing.AverageMarksPercent = row.AverageMarksPercent;
            existing.RiskScore = score;
            existing.RiskLevel = level;
            existing.AttendanceFlag = attFlag;
            existing.MarksFlag = marksFlag;
            existing.CombinedFlag = attFlag && marksFlag;
            existing.ComputedAt = now;

            if (level is RiskLevel.High or RiskLevel.Critical)
                highCritical++;
            total++;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Result<ComputeAtRiskResult>.Success(new ComputeAtRiskResult(total, highCritical));
    }

    internal static (decimal Score, RiskLevel Level, bool AttFlag, bool MarksFlag) ComputeAtRiskScore(
        decimal attendancePercent, decimal averageMarksPercent)
    {
        var attScore = Math.Max(0m, 100m - attendancePercent);
        var marksScore = Math.Max(0m, 100m - averageMarksPercent);
        var riskScore = (attScore * 0.4m) + (marksScore * 0.6m);

        var level = riskScore switch
        {
            < 25m => RiskLevel.Low,
            < 50m => RiskLevel.Medium,
            < 75m => RiskLevel.High,
            _ => RiskLevel.Critical
        };

        return (riskScore, level, attendancePercent < 75m, averageMarksPercent < 50m);
    }

    private class StudentRiskRow
    {
        public Guid StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string ProgramName { get; set; } = string.Empty;
        public decimal AttendancePercent { get; set; }
        public decimal AverageMarksPercent { get; set; }
    }
}
