using ERP.Accreditation.Infrastructure;
using ERP.NAAC.Domain;
using ERP.NAAC.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.NAAC.Application.Queries;

public record GetCriterionDashboardQuery(string CriterionNumber, int AcademicYear) : IRequest<Result<CriterionDashboardDto>>;

public record CriterionDashboardDto(
    string CriterionNumber,
    string CriterionTitle,
    IReadOnlyList<IndicatorStatusDto> Indicators);

public record IndicatorStatusDto(
    string IndicatorNumber,
    int TaggedEvidenceCount,
    decimal? KeyMetricValue,
    string? KeyMetricLabel,
    bool HasSsrContent);

public class GetCriterionDashboardHandler : IRequestHandler<GetCriterionDashboardQuery, Result<CriterionDashboardDto>>
{
    private readonly INaacDbContext _naacDb;
    private readonly IAccreditationDbContext _accreditationDb;

    public GetCriterionDashboardHandler(INaacDbContext naacDb, IAccreditationDbContext accreditationDb)
    {
        _naacDb = naacDb;
        _accreditationDb = accreditationDb;
    }

    public async Task<Result<CriterionDashboardDto>> Handle(GetCriterionDashboardQuery request, CancellationToken cancellationToken)
    {
        var criterion = NaacCriteria.All.FirstOrDefault(c => c.Number == request.CriterionNumber);
        if (criterion is null)
            return Result<CriterionDashboardDto>.Failure($"Criterion {request.CriterionNumber} not found.");

        var tagCounts = await _accreditationDb.EvidenceTags
            .Where(t => t.NaacCriterion == request.CriterionNumber)
            .GroupBy(t => t.NaacIndicator)
            .Select(g => new { Indicator = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var summaries = await _accreditationDb.EvidenceSummaries
            .Where(s => s.AcademicYear == request.AcademicYear)
            .ToListAsync(cancellationToken);

        var ssrSections = await _naacDb.SsrSections
            .Where(s => s.CriterionNumber == request.CriterionNumber)
            .Select(s => new { s.IndicatorNumber, HasContent = s.Content != null && s.Content.Length > 0 })
            .ToListAsync(cancellationToken);

        var indicators = criterion.Indicators.Select(ind =>
        {
            var count = tagCounts.FirstOrDefault(t => t.Indicator == ind)?.Count ?? 0;
            var hasSsr = ssrSections.Any(s => s.IndicatorNumber == ind && s.HasContent);
            var (metricValue, metricLabel) = ResolveKeyMetric(ind, summaries.Select(s =>
                new SummaryEntry(s.Module, s.Category, s.MetricKey, s.NumericValue)).ToList());

            return new IndicatorStatusDto(ind, count, metricValue, metricLabel, hasSsr);
        }).ToList();

        return Result<CriterionDashboardDto>.Success(
            new CriterionDashboardDto(criterion.Number, criterion.Title, indicators));
    }

    // Maps known NAAC indicators to evidence summary metric keys
    private static (decimal? Value, string? Label) ResolveKeyMetric(
        string indicator, List<SummaryEntry> summaries)
    {
        return indicator switch
        {
            "2.6" => FindMetric(summaries, "Exams", "Results", "pass_percent", "Pass %"),
            "2.1" => FindMetric(summaries, "SIS", "Enrollment", "total_enrolled", "Total Enrolled"),
            "5.2" => FindMetric(summaries, "Placement", "Offers", "placement_percent", "Placement %"),
            "3.3" => FindMetric(summaries, "Research", "Output", "total_publications", "Publications"),
            _ => (null, null)
        };
    }

    private static (decimal? Value, string? Label) FindMetric(
        List<SummaryEntry> summaries, string module, string category, string key, string label)
    {
        var entry = summaries.FirstOrDefault(s =>
            s.Module == module && s.Category == category && s.MetricKey == key);
        return entry is null ? (null, null) : (entry.NumericValue, label);
    }

    private record SummaryEntry(string Module, string Category, string MetricKey, decimal? NumericValue);
}
