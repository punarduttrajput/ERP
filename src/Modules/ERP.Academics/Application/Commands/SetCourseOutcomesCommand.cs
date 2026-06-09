using ERP.Academics.Domain;
using ERP.Academics.Infrastructure;
using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Academics.Application.Commands;

public record OutcomeItem(string Code, string Description);

public record SetCourseOutcomesCommand(Guid SubjectId, IReadOnlyList<OutcomeItem> Outcomes) : IRequest<Result>;

public class SetCourseOutcomesHandler : IRequestHandler<SetCourseOutcomesCommand, Result>
{
    private readonly IAcademicsDbContext _db;
    private readonly ICurrentTenant _currentTenant;

    public SetCourseOutcomesHandler(IAcademicsDbContext db, ICurrentTenant currentTenant)
    {
        _db = db;
        _currentTenant = currentTenant;
    }

    public async Task<Result> Handle(SetCourseOutcomesCommand request, CancellationToken cancellationToken)
    {
        if (request.Outcomes.Count > 12)
            return Result.Failure("A subject may have at most 12 course outcomes.");

        var tenantId = _currentTenant.TenantId ?? Guid.Empty;

        var subjectExists = await _db.Subjects
            .AnyAsync(x => x.Id == request.SubjectId && x.TenantId == tenantId && !x.IsDeleted, cancellationToken);

        if (!subjectExists)
            return Result.Failure("Subject not found.");

        var existing = await _db.CourseOutcomes
            .Where(x => x.SubjectId == request.SubjectId && x.TenantId == tenantId && !x.IsDeleted)
            .ToListAsync(cancellationToken);

        // Hard delete is intentional here: CO replacement is an admin operation, not soft-deletable history.
        _db.CourseOutcomes.RemoveRange(existing);

        foreach (var outcome in request.Outcomes)
        {
            _db.CourseOutcomes.Add(new CourseOutcome
            {
                TenantId = tenantId,
                SubjectId = request.SubjectId,
                Code = outcome.Code,
                Description = outcome.Description
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
