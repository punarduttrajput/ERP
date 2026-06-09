using ERP.HRMS.Domain;
using ERP.HRMS.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.HRMS.Application.Commands;

public record SubmitSelfAssessmentCommand(
    Guid AppraisalId,
    Guid TenantId,
    string Assessment,
    decimal Rating
) : IRequest<Result>;

public class SubmitSelfAssessmentHandler : IRequestHandler<SubmitSelfAssessmentCommand, Result>
{
    private readonly IHrmsDbContext _db;

    public SubmitSelfAssessmentHandler(IHrmsDbContext db)
    {
        _db = db;
    }

    public async Task<Result> Handle(SubmitSelfAssessmentCommand request, CancellationToken cancellationToken)
    {
        var appraisal = await _db.Appraisals
            .FirstOrDefaultAsync(a => a.Id == request.AppraisalId && a.TenantId == request.TenantId, cancellationToken);

        if (appraisal is null)
            return Result.Failure("Appraisal not found.");

        if (appraisal.Status != AppraisalStatus.SelfAssessmentPending)
            return Result.Failure("Appraisal is not in the self-assessment stage.");

        appraisal.SelfAssessment = request.Assessment;
        appraisal.SelfRating = request.Rating;
        appraisal.Status = AppraisalStatus.ManagerReviewPending;

        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
