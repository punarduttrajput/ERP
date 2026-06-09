using ERP.HRMS.Domain;
using ERP.HRMS.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.HRMS.Application.Commands;

public record FinaliseAppraisalCommand(
    Guid AppraisalId,
    Guid TenantId,
    string HrComments,
    decimal FinalRating,
    Guid ReviewedBy
) : IRequest<Result>;

public class FinaliseAppraisalHandler : IRequestHandler<FinaliseAppraisalCommand, Result>
{
    private readonly IHrmsDbContext _db;

    public FinaliseAppraisalHandler(IHrmsDbContext db)
    {
        _db = db;
    }

    public async Task<Result> Handle(FinaliseAppraisalCommand request, CancellationToken cancellationToken)
    {
        var appraisal = await _db.Appraisals
            .FirstOrDefaultAsync(a => a.Id == request.AppraisalId && a.TenantId == request.TenantId, cancellationToken);

        if (appraisal is null)
            return Result.Failure("Appraisal not found.");

        if (appraisal.Status != AppraisalStatus.HrReviewPending)
            return Result.Failure("Appraisal is not in the HR review stage.");

        appraisal.HrComments = request.HrComments;
        appraisal.FinalRating = request.FinalRating;
        appraisal.FinalReviewedBy = request.ReviewedBy;
        appraisal.FinalReviewedAt = DateTime.UtcNow;
        appraisal.Status = AppraisalStatus.Finalised;

        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
