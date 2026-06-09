using ERP.OBE.Domain;
using ERP.OBE.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.OBE.Application.Commands;

public record ComputeIndirectAttainmentCommand(Guid TenantId, Guid SurveyId) : IRequest<Result>;

public class ComputeIndirectAttainmentHandler : IRequestHandler<ComputeIndirectAttainmentCommand, Result>
{
    private readonly IObeDbContext _db;

    public ComputeIndirectAttainmentHandler(IObeDbContext db) => _db = db;

    public async Task<Result> Handle(ComputeIndirectAttainmentCommand request, CancellationToken cancellationToken)
    {
        var survey = await _db.IndirectSurveys
            .Include(x => x.Questions)
            .FirstOrDefaultAsync(x => x.Id == request.SurveyId, cancellationToken);

        if (survey is null)
            return Result.Failure("Survey not found.");

        var responses = await _db.SurveyResponses
            .Where(x => x.SurveyId == request.SurveyId)
            .ToListAsync(cancellationToken);

        if (responses.Count == 0)
            return Result.Failure("No responses to aggregate.");

        var questionMap = survey.Questions.ToDictionary(q => q.Id, q => q.CourseOutcomeCode);

        // Average score per CO: avg_score / 5.0 * 100 converts Likert 1-5 to percent
        var coScores = responses
            .Where(r => questionMap.ContainsKey(r.QuestionId))
            .GroupBy(r => questionMap[r.QuestionId])
            .ToDictionary(g => g.Key, g => g.Average(r => (decimal)r.Score) / 5.0m * 100m);

        survey.AggregatedScore = coScores.Count > 0
            ? Math.Round(coScores.Values.Average(), 2)
            : 0m;
        survey.ClosedAt = DateTime.UtcNow;

        foreach (var kvp in coScores)
        {
            string coCode = kvp.Key;
            decimal indirectPercent = kvp.Value;

            var direct = await _db.DirectAttainments.FirstOrDefaultAsync(
                x => x.TenantId == request.TenantId
                  && x.SubjectId == survey.SubjectId
                  && x.CourseOutcomeCode == coCode
                  && x.SemesterId == survey.SemesterId,
                cancellationToken);

            if (direct is null)
                continue;

            var combined = Math.Round(0.8m * direct.AttainmentPercent + 0.2m * indirectPercent, 2);
            const decimal target = 60m;
            var gap = Math.Round(target - combined, 2);
            var level = ComputeDirectAttainmentHandler.ComputeLevel(combined);

            var existingGap = await _db.AttainmentGaps.FirstOrDefaultAsync(
                x => x.TenantId == request.TenantId
                  && x.SubjectId == survey.SubjectId
                  && x.CourseOutcomeCode == coCode
                  && x.SemesterId == survey.SemesterId,
                cancellationToken);

            if (existingGap is not null)
            {
                existingGap.IndirectAttainmentPercent = Math.Round(indirectPercent, 2);
                existingGap.CombinedAttainmentPercent = combined;
                existingGap.GapPercent = gap;
                existingGap.Level = level;
            }
            else
            {
                _db.AttainmentGaps.Add(new AttainmentGap
                {
                    TenantId = request.TenantId,
                    SubjectId = survey.SubjectId,
                    CourseOutcomeCode = coCode,
                    SemesterId = survey.SemesterId,
                    AcademicYear = survey.AcademicYear,
                    DirectAttainmentPercent = direct.AttainmentPercent,
                    IndirectAttainmentPercent = Math.Round(indirectPercent, 2),
                    CombinedAttainmentPercent = combined,
                    TargetPercent = target,
                    GapPercent = gap,
                    Level = level
                });
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
