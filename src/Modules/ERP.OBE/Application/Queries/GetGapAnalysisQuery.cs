using ERP.OBE.Domain;
using ERP.OBE.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.OBE.Application.Queries;

public record GetGapAnalysisQuery(Guid TenantId, Guid SubjectId, Guid SemesterId)
    : IRequest<Result<GapAnalysisDto>>;

public record GapItemDto(
    Guid GapId,
    string CourseOutcomeCode,
    decimal DirectAttainmentPercent,
    decimal? IndirectAttainmentPercent,
    decimal CombinedAttainmentPercent,
    decimal TargetPercent,
    decimal GapPercent,
    AttainmentLevel Level,
    IReadOnlyList<ActionPlanDto> ActionPlans);

public record ActionPlanDto(
    Guid Id,
    string Description,
    ActionPlanStatus Status,
    string? Outcome,
    Guid? AssignedTo,
    DateOnly? TargetDate);

public record GapAnalysisDto(
    Guid SubjectId,
    Guid SemesterId,
    IReadOnlyList<GapItemDto> Gaps);

public class GetGapAnalysisHandler : IRequestHandler<GetGapAnalysisQuery, Result<GapAnalysisDto>>
{
    private readonly IObeDbContext _db;

    public GetGapAnalysisHandler(IObeDbContext db) => _db = db;

    public async Task<Result<GapAnalysisDto>> Handle(GetGapAnalysisQuery request, CancellationToken cancellationToken)
    {
        var gaps = await _db.AttainmentGaps
            .Where(x => x.TenantId == request.TenantId
                     && x.SubjectId == request.SubjectId
                     && x.SemesterId == request.SemesterId
                     && !x.IsDeleted)
            .ToListAsync(cancellationToken);

        var gapIds = gaps.Select(g => g.Id).ToList();
        var plans = await _db.ActionPlans
            .Where(x => x.TenantId == request.TenantId && gapIds.Contains(x.GapId) && !x.IsDeleted)
            .ToListAsync(cancellationToken);

        var plansByGap = plans.GroupBy(p => p.GapId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var items = gaps.Select(gap =>
        {
            plansByGap.TryGetValue(gap.Id, out var gapPlans);
            var planDtos = (gapPlans ?? new List<ActionPlan>())
                .Select(p => new ActionPlanDto(p.Id, p.Description, p.Status, p.Outcome, p.AssignedTo, p.TargetDate))
                .ToList();
            return new GapItemDto(
                gap.Id,
                gap.CourseOutcomeCode,
                gap.DirectAttainmentPercent,
                gap.IndirectAttainmentPercent,
                gap.CombinedAttainmentPercent,
                gap.TargetPercent,
                gap.GapPercent,
                gap.Level,
                planDtos);
        }).ToList();

        return Result<GapAnalysisDto>.Success(new GapAnalysisDto(request.SubjectId, request.SemesterId, items));
    }
}
