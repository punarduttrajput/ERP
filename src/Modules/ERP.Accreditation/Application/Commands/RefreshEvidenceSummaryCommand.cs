using ERP.Accreditation.Domain;
using ERP.Accreditation.Infrastructure;
using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Common;
using ERP.Shared.Application.Contracts;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Accreditation.Application.Commands;

public record RefreshEvidenceSummaryCommand(
    Guid TenantId,
    int AcademicYear,
    string[]? ModuleFilter
) : IRequest<Result<int>>;

public class RefreshEvidenceSummaryHandler : IRequestHandler<RefreshEvidenceSummaryCommand, Result<int>>
{
    private readonly IAccreditationDbContext _db;
    private readonly IEnumerable<IEvidenceProvider> _providers;

    public RefreshEvidenceSummaryHandler(IAccreditationDbContext db, IEnumerable<IEvidenceProvider> providers)
    {
        _db = db;
        _providers = providers;
    }

    public async Task<Result<int>> Handle(RefreshEvidenceSummaryCommand request, CancellationToken cancellationToken)
    {
        var activeProviders = request.ModuleFilter is { Length: > 0 }
            ? _providers.Where(p => request.ModuleFilter.Contains(p.ModuleName))
            : _providers;

        var upsertCount = 0;

        foreach (var provider in activeProviders)
        {
            IReadOnlyList<EvidenceItem> items;
            try
            {
                items = await provider.GetEvidenceAsync(request.TenantId, request.AcademicYear, cancellationToken);
            }
            catch
            {
                // Individual provider failures must not abort the refresh for all other providers.
                continue;
            }

            var grouped = items
                .GroupBy(i => new { i.Module, i.Category })
                .ToList();

            foreach (var group in grouped)
            {
                var numericItems = group.Where(i => i.NumericValue.HasValue).ToList();

                var metricsToUpsert = new List<(string Key, decimal? Numeric, string? Text)>();

                metricsToUpsert.Add(("count", group.Count(), null));

                if (numericItems.Count > 0)
                {
                    metricsToUpsert.Add(("sum", numericItems.Sum(i => i.NumericValue!.Value), null));
                    metricsToUpsert.Add(("avg", numericItems.Average(i => i.NumericValue!.Value), null));
                }

                foreach (var (key, numeric, text) in metricsToUpsert)
                {
                    var metricKey = $"{group.Key.Category}_{key}";

                    var existing = await _db.EvidenceSummaries
                        .FirstOrDefaultAsync(
                            s => s.TenantId == request.TenantId
                              && s.AcademicYear == request.AcademicYear
                              && s.Module == group.Key.Module
                              && s.Category == group.Key.Category
                              && s.MetricKey == metricKey,
                            cancellationToken);

                    if (existing is null)
                    {
                        existing = new EvidenceSummary
                        {
                            TenantId = request.TenantId,
                            AcademicYear = request.AcademicYear,
                            Module = group.Key.Module,
                            Category = group.Key.Category,
                            MetricKey = metricKey
                        };
                        _db.EvidenceSummaries.Add(existing);
                    }

                    existing.NumericValue = numeric;
                    existing.TextValue = text;
                    existing.ComputedAt = DateTime.UtcNow;

                    upsertCount++;
                }
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success(upsertCount);
    }
}
