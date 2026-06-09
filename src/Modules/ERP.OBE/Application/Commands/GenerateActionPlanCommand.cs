using ERP.OBE.Domain;
using ERP.OBE.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.OBE.Application.Commands;

public record GenerateActionPlanCommand(
    Guid TenantId,
    Guid SubjectId,
    Guid SemesterId,
    int AcademicYear) : IRequest<Result<int>>;

public class GenerateActionPlanHandler : IRequestHandler<GenerateActionPlanCommand, Result<int>>
{
    private readonly IObeDbContext _db;

    public GenerateActionPlanHandler(IObeDbContext db) => _db = db;

    public async Task<Result<int>> Handle(GenerateActionPlanCommand request, CancellationToken cancellationToken)
    {
        var gaps = await _db.AttainmentGaps
            .Where(x => x.TenantId == request.TenantId
                     && x.SubjectId == request.SubjectId
                     && x.SemesterId == request.SemesterId
                     && x.GapPercent > 0)
            .ToListAsync(cancellationToken);

        if (gaps.Count == 0)
            return Result<int>.Success(0);

        int created = 0;

        foreach (var gap in gaps)
        {
            var description = gap.Level == AttainmentLevel.NotMet
                ? $"Conduct remedial classes for {gap.CourseOutcomeCode}. Review teaching methodology. Increase practice problems."
                : $"Provide additional study materials for {gap.CourseOutcomeCode}. Schedule peer tutoring sessions.";

            _db.ActionPlans.Add(new ActionPlan
            {
                TenantId = request.TenantId,
                GapId = gap.Id,
                SubjectId = request.SubjectId,
                CourseOutcomeCode = gap.CourseOutcomeCode,
                Description = description,
                Status = ActionPlanStatus.Open
            });

            created++;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Result<int>.Success(created);
    }
}
