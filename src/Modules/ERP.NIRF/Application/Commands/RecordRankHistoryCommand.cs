using ERP.NIRF.Domain;
using ERP.NIRF.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;

namespace ERP.NIRF.Application.Commands;

public record RecordRankHistoryCommand(
    Guid TenantId,
    int RankingYear,
    string Category,
    int? Rank,
    decimal? Score,
    decimal? TeachingLearningScore,
    decimal? ResearchScore,
    decimal? GraduationOutcomesScore,
    decimal? OutreachScore,
    decimal? PerceptionScore) : IRequest<Result>;

public class RecordRankHistoryHandler : IRequestHandler<RecordRankHistoryCommand, Result>
{
    private readonly INirfDbContext _db;

    public RecordRankHistoryHandler(INirfDbContext db) => _db = db;

    public async Task<Result> Handle(RecordRankHistoryCommand request, CancellationToken cancellationToken)
    {
        var existing = _db.NirfRankHistory
            .FirstOrDefault(r => r.TenantId == request.TenantId
                && r.RankingYear == request.RankingYear
                && r.Category == request.Category
                && !r.IsDeleted);

        if (existing is not null)
        {
            existing.Rank = request.Rank;
            existing.Score = request.Score;
            existing.TeachingLearningScore = request.TeachingLearningScore;
            existing.ResearchScore = request.ResearchScore;
            existing.GraduationOutcomesScore = request.GraduationOutcomesScore;
            existing.OutreachScore = request.OutreachScore;
            existing.PerceptionScore = request.PerceptionScore;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            _db.NirfRankHistory.Add(new NirfRankEntry
            {
                TenantId = request.TenantId,
                RankingYear = request.RankingYear,
                Category = request.Category,
                Rank = request.Rank,
                Score = request.Score,
                TeachingLearningScore = request.TeachingLearningScore,
                ResearchScore = request.ResearchScore,
                GraduationOutcomesScore = request.GraduationOutcomesScore,
                OutreachScore = request.OutreachScore,
                PerceptionScore = request.PerceptionScore
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
