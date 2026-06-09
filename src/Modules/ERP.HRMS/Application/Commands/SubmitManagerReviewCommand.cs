using ERP.HRMS.Domain;
using ERP.HRMS.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.HRMS.Application.Commands;

public record SubmitManagerReviewCommand(
    Guid AppraisalId,
    Guid TenantId,
    string Review,
    decimal Rating
) : IRequest<Result>;

public class SubmitManagerReviewHandler : IRequestHandler<SubmitManagerReviewCommand, Result>
{
    private readonly IHrmsDbContext _db;

    public SubmitManagerReviewHandler(IHrmsDbContext db)
    {
        _db = db;
    }

    public async Task<Result> Handle(SubmitManagerReviewCommand request, CancellationToken cancellationToken)
    {
        var appraisal = await _db.Appraisals
            .FirstOrDefaultAsync(a => a.Id == request.AppraisalId && a.TenantId == request.TenantId, cancellationToken);

        if (appraisal is null)
            return Result.Failure("Appraisal not found.");

        if (appraisal.Status != AppraisalStatus.ManagerReviewPending)
            return Result.Failure("Appraisal is not in the manager review stage.");

        appraisal.ManagerReview = request.Review;
        appraisal.ManagerRating = request.Rating;
        appraisal.Status = AppraisalStatus.HrReviewPending;

        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
