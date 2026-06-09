using Dapper;
using ERP.NIRF.Domain;
using ERP.NIRF.Infrastructure;
using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Common;
using MediatR;
using System.Text.Json;

namespace ERP.NIRF.Application.Commands;

public record CompileNirfDataCommand(Guid TenantId, int RankingYear, string Category) : IRequest<Result<Guid>>;

public class CompileNirfDataHandler : IRequestHandler<CompileNirfDataCommand, Result<Guid>>
{
    private const decimal WeightTL = 0.30m;
    private const decimal WeightR  = 0.30m;
    private const decimal WeightGO = 0.20m;
    private const decimal WeightO  = 0.10m;
    private const decimal WeightP  = 0.10m;

    private readonly INirfDbContext _db;
    private readonly IDbConnectionFactory _connectionFactory;

    public CompileNirfDataHandler(INirfDbContext db, IDbConnectionFactory connectionFactory)
    {
        _db = db;
        _connectionFactory = connectionFactory;
    }

    public async Task<Result<Guid>> Handle(CompileNirfDataCommand request, CancellationToken cancellationToken)
    {
        using var conn = await _connectionFactory.CreateReadConnectionAsync(cancellationToken);

        var facultyCount = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM employees WHERE TenantId = @TenantId AND Status = 1 AND IsDeleted = 0",
            new { request.TenantId });

        var studentCount = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM students WHERE TenantId = @TenantId AND AcademicYear = @Year AND IsActive = 1 AND IsDeleted = 0",
            new { request.TenantId, Year = request.RankingYear });

        decimal ratio = facultyCount > 0 ? (decimal)studentCount / facultyCount : 999m;
        // Score is full (100) when ratio <= 15; degrades linearly beyond that
        decimal tlRaw = ratio <= 15m ? 100m : Math.Round(100m * (15m / ratio), 2);
        var tlData = new { FacultyCount = facultyCount, StudentCount = studentCount, StudentFacultyRatio = Math.Round(ratio, 2) };

        var researchCount = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM student_results WHERE TenantId = @TenantId AND IsPublished = 1 AND IsDeleted = 0",
            new { request.TenantId });
        decimal rRaw = Math.Min(100m, researchCount * 2m);
        var rData = new { PublishedResultsCount = researchCount };

        var examStats = await conn.QuerySingleOrDefaultAsync<(int Total, int Passed)>(
            @"SELECT COUNT(*) AS Total, SUM(CASE WHEN sr.Status = 1 THEN 1 ELSE 0 END) AS Passed
              FROM student_results sr
              JOIN semesters s ON sr.SemesterId = s.Id
              WHERE sr.TenantId = @TenantId AND YEAR(s.StartDate) = @Year
                AND sr.IsPublished = 1 AND sr.IsDeleted = 0 AND s.IsDeleted = 0",
            new { request.TenantId, Year = request.RankingYear });

        decimal passRate = examStats.Total > 0 ? (decimal)examStats.Passed / examStats.Total * 100m : 0m;

        var totalStudents = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(DISTINCT StudentId) FROM placement_offers WHERE TenantId = @TenantId AND IsDeleted = 0",
            new { request.TenantId });

        var placedCount = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(DISTINCT StudentId) FROM placement_offers WHERE TenantId = @TenantId AND Status IN (1, 2) AND IsDeleted = 0",
            new { request.TenantId });

        decimal placementRate = totalStudents > 0 ? (decimal)placedCount / totalStudents * 100m : 0m;
        decimal goRaw = Math.Round(passRate * 0.5m + placementRate * 0.5m, 2);
        var goData = new { TotalExamResults = examStats.Total, PassedCount = examStats.Passed, PassRate = Math.Round(passRate, 2), PlacedStudents = placedCount, PlacementRate = Math.Round(placementRate, 2) };

        var categoryStats = await conn.QuerySingleOrDefaultAsync<(int ScSt, int Obc, int Total)>(
            @"SELECT SUM(CASE WHEN Category IN ('SC','ST') THEN 1 ELSE 0 END) AS ScSt,
                     SUM(CASE WHEN Category = 'OBC' THEN 1 ELSE 0 END) AS Obc,
                     COUNT(*) AS Total
              FROM students
              WHERE TenantId = @TenantId AND AcademicYear = @Year AND IsActive = 1 AND IsDeleted = 0",
            new { request.TenantId, Year = request.RankingYear });

        decimal scStObcPercent = categoryStats.Total > 0
            ? (decimal)(categoryStats.ScSt + categoryStats.Obc) / categoryStats.Total * 100m
            : 0m;
        decimal oRaw = Math.Min(100m, Math.Round(scStObcPercent, 2));
        var oData = new { ScStCount = categoryStats.ScSt, ObcCount = categoryStats.Obc, TotalStudents = categoryStats.Total, ScStObcPercent = Math.Round(scStObcPercent, 2) };

        // Perception starts at 50 (industry average) until manually overridden
        decimal pRaw = 50.0m;
        var pData = new { Note = "Industry average default. Override via PATCH endpoint." };

        var scores = new[]
        {
            (Parameter: NirfParameter.TeachingLearning, Raw: tlRaw, Weight: WeightTL, Data: (object)tlData),
            (Parameter: NirfParameter.Research,         Raw: rRaw,  Weight: WeightR,  Data: (object)rData),
            (Parameter: NirfParameter.GraduationOutcomes, Raw: goRaw, Weight: WeightGO, Data: (object)goData),
            (Parameter: NirfParameter.Outreach,         Raw: oRaw,  Weight: WeightO,  Data: (object)oData),
            (Parameter: NirfParameter.Perception,       Raw: pRaw,  Weight: WeightP,  Data: (object)pData),
        };

        var existing = _db.NirfSubmissions
            .FirstOrDefault(s => s.TenantId == request.TenantId && s.RankingYear == request.RankingYear && !s.IsDeleted);

        NirfSubmission submission;
        if (existing is not null)
        {
            submission = existing;
            submission.Category = request.Category;
            submission.Status = SubmissionStatus.Compiled;
            submission.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            submission = new NirfSubmission
            {
                TenantId = request.TenantId,
                RankingYear = request.RankingYear,
                Category = request.Category,
                Status = SubmissionStatus.Compiled
            };
            _db.NirfSubmissions.Add(submission);
        }

        await _db.SaveChangesAsync(cancellationToken);

        foreach (var (parameter, raw, weight, data) in scores)
        {
            var existingScore = _db.NirfParameterScores
                .FirstOrDefault(p => p.TenantId == request.TenantId
                    && p.SubmissionId == submission.Id
                    && p.Parameter == parameter
                    && !p.IsManualOverride
                    && !p.IsDeleted);

            if (existingScore is not null)
            {
                existingScore.RawScore = raw;
                existingScore.WeightedScore = Math.Round(raw * weight, 2);
                existingScore.Weight = weight;
                existingScore.DataJson = JsonSerializer.Serialize(data);
                existingScore.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                var manualExists = _db.NirfParameterScores
                    .Any(p => p.TenantId == request.TenantId
                        && p.SubmissionId == submission.Id
                        && p.Parameter == parameter
                        && p.IsManualOverride
                        && !p.IsDeleted);

                if (!manualExists)
                {
                    _db.NirfParameterScores.Add(new NirfParameterScore
                    {
                        TenantId = request.TenantId,
                        SubmissionId = submission.Id,
                        Parameter = parameter,
                        RawScore = raw,
                        WeightedScore = Math.Round(raw * weight, 2),
                        Weight = weight,
                        DataJson = JsonSerializer.Serialize(data),
                        IsManualOverride = false
                    });
                }
            }
        }

        await _db.SaveChangesAsync(cancellationToken);

        var allScores = _db.NirfParameterScores
            .Where(p => p.TenantId == request.TenantId && p.SubmissionId == submission.Id && !p.IsDeleted)
            .ToList();

        submission.OverallScore = Math.Round(allScores.Sum(s => s.WeightedScore), 2);
        submission.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(submission.Id);
    }
}
