using ERP.Accreditation.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Accreditation.Application.Queries;

public record GetEvidenceCoverageQuery(Guid TenantId) : IRequest<IReadOnlyList<CriterionCoverageDto>>;

public record CriterionCoverageDto(
    string Criterion,
    int TotalIndicators,
    int TaggedIndicators,
    decimal CoveragePercent
);

public class GetEvidenceCoverageHandler : IRequestHandler<GetEvidenceCoverageQuery, IReadOnlyList<CriterionCoverageDto>>
{
    // NAAC 7-criteria key indicator counts per criterion.
    private static readonly Dictionary<string, int> CriterionIndicatorCounts = new()
    {
        { "1", 3 },
        { "2", 4 },
        { "3", 4 },
        { "4", 3 },
        { "5", 4 },
        { "6", 4 },
        { "7", 3 }
    };

    private readonly IAccreditationDbContext _db;

    public GetEvidenceCoverageHandler(IAccreditationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<CriterionCoverageDto>> Handle(GetEvidenceCoverageQuery request, CancellationToken cancellationToken)
    {
        var taggedIndicatorsByCriterion = await _db.EvidenceTags
            .Where(t => t.TenantId == request.TenantId && !t.IsDeleted)
            .GroupBy(t => t.NaacCriterion)
            .Select(g => new
            {
                Criterion = g.Key,
                TaggedIndicators = g.Select(t => t.NaacIndicator).Distinct().Count()
            })
            .ToListAsync(cancellationToken);

        var taggedMap = taggedIndicatorsByCriterion.ToDictionary(x => x.Criterion, x => x.TaggedIndicators);

        var result = new List<CriterionCoverageDto>();

        foreach (var (criterionNum, totalIndicators) in CriterionIndicatorCounts.OrderBy(k => k.Key))
        {
            // Criterion stored as "X.Y" — match on the first segment.
            var tagged = taggedMap
                .Where(kvp => kvp.Key.StartsWith(criterionNum + "."))
                .Sum(kvp => kvp.Value);

            // Cap at totalIndicators — multiple tags on same indicator still count once.
            var cappedTagged = Math.Min(tagged, totalIndicators);
            var coverage = totalIndicators > 0
                ? Math.Round((decimal)cappedTagged / totalIndicators * 100, 2)
                : 0m;

            result.Add(new CriterionCoverageDto(criterionNum, totalIndicators, cappedTagged, coverage));
        }

        return result;
    }
}
