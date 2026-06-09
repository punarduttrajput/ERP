using Dapper;
using ERP.Analytics.Domain;
using ERP.Analytics.Infrastructure;
using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Analytics.Application.Commands;

public record ComputeFeeDefaultRiskCommand(Guid TenantId, int AcademicYear)
    : IRequest<Result<int>>;

public class ComputeFeeDefaultRiskHandler : IRequestHandler<ComputeFeeDefaultRiskCommand, Result<int>>
{
    private readonly IAnalyticsDbContext _db;
    private readonly IDbConnectionFactory _connectionFactory;

    public ComputeFeeDefaultRiskHandler(IAnalyticsDbContext db, IDbConnectionFactory connectionFactory)
    {
        _db = db;
        _connectionFactory = connectionFactory;
    }

    public async Task<Result<int>> Handle(ComputeFeeDefaultRiskCommand request, CancellationToken cancellationToken)
    {
        using var conn = await _connectionFactory.CreateReadConnectionAsync(cancellationToken);

        const string sql = @"
            SELECT
                s.Id AS StudentId,
                CONCAT(s.FirstName, ' ', s.LastName) AS StudentName,
                sfa.DueAmount AS TotalDue,
                COALESCE(DATEDIFF(CURDATE(), MIN(fi.DueDate)), 0) AS OverdueDays,
                (SELECT COUNT(*) FROM student_fee_accounts prev
                 WHERE prev.StudentId = s.Id AND prev.AcademicYear < @AcademicYear
                   AND prev.IsFullyPaid = 0 AND prev.IsDeleted = 0) AS PreviousDefaultCount
            FROM students s
            JOIN student_fee_accounts sfa ON sfa.StudentId = s.Id AND sfa.AcademicYear = @AcademicYear
            LEFT JOIN fee_installments fi ON fi.AccountId = sfa.Id AND fi.IsPaid = 0 AND fi.IsDeleted = 0
            WHERE s.TenantId = @TenantId AND sfa.DueAmount > 0
              AND s.IsActive = 1 AND s.IsDeleted = 0 AND sfa.IsDeleted = 0
            GROUP BY s.Id, s.FirstName, s.LastName, sfa.DueAmount";

        var rows = await conn.QueryAsync<FeeRiskRow>(sql, new { request.TenantId, request.AcademicYear });

        var now = DateTime.UtcNow;
        int total = 0;

        foreach (var row in rows)
        {
            var (score, level) = ComputeFeeDefaultScore(row.TotalDue, row.OverdueDays, row.PreviousDefaultCount);

            var existing = await _db.FeeDefaultRiskScores
                .FirstOrDefaultAsync(x => x.TenantId == request.TenantId
                    && x.StudentId == row.StudentId
                    && x.AcademicYear == request.AcademicYear, cancellationToken);

            if (existing is null)
            {
                existing = new FeeDefaultRiskScore
                {
                    TenantId = request.TenantId,
                    StudentId = row.StudentId,
                    AcademicYear = request.AcademicYear
                };
                _db.FeeDefaultRiskScores.Add(existing);
            }

            existing.StudentName = row.StudentName;
            existing.TotalDue = row.TotalDue;
            existing.OverdueDays = row.OverdueDays;
            existing.PreviousDefaultCount = row.PreviousDefaultCount;
            existing.RiskScore = score;
            existing.RiskLevel = level;
            existing.ComputedAt = now;

            total++;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Result<int>.Success(total);
    }

    internal static (decimal Score, RiskLevel Level) ComputeFeeDefaultScore(
        decimal totalDue, int overdueDays, int previousDefaultCount)
    {
        var dueFactor = Math.Min(100m, (totalDue / 10000m) * 20m);
        var overdueFactor = Math.Min(100m, overdueDays * 2m);
        var historyFactor = Math.Min(100m, previousDefaultCount * 25m);
        var riskScore = (dueFactor * 0.4m) + (overdueFactor * 0.4m) + (historyFactor * 0.2m);

        var level = riskScore switch
        {
            < 25m => RiskLevel.Low,
            < 50m => RiskLevel.Medium,
            < 75m => RiskLevel.High,
            _ => RiskLevel.Critical
        };

        return (riskScore, level);
    }

    private class FeeRiskRow
    {
        public Guid StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public decimal TotalDue { get; set; }
        public int OverdueDays { get; set; }
        public int PreviousDefaultCount { get; set; }
    }
}
