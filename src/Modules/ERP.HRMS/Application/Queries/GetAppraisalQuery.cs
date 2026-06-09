using ERP.HRMS.Domain;
using ERP.HRMS.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.HRMS.Application.Queries;

public record GetAppraisalQuery(Guid AppraisalId, Guid TenantId) : IRequest<Result<AppraisalDto>>;

public record AppraisalDto(
    Guid Id,
    Guid EmployeeId,
    int ReviewYear,
    AppraisalStatus Status,
    string? SelfAssessment,
    decimal? SelfRating,
    string? ManagerReview,
    decimal? ManagerRating,
    string? HrComments,
    decimal? FinalRating,
    Guid? FinalReviewedBy,
    DateTime? FinalReviewedAt
);

public class GetAppraisalHandler : IRequestHandler<GetAppraisalQuery, Result<AppraisalDto>>
{
    private readonly IHrmsDbContext _db;

    public GetAppraisalHandler(IHrmsDbContext db)
    {
        _db = db;
    }

    public async Task<Result<AppraisalDto>> Handle(GetAppraisalQuery request, CancellationToken cancellationToken)
    {
        var a = await _db.Appraisals
            .FirstOrDefaultAsync(x => x.Id == request.AppraisalId && x.TenantId == request.TenantId, cancellationToken);

        if (a is null)
            return Result.Failure<AppraisalDto>("Appraisal not found.");

        return Result.Success(new AppraisalDto(
            a.Id, a.EmployeeId, a.ReviewYear, a.Status,
            a.SelfAssessment, a.SelfRating,
            a.ManagerReview, a.ManagerRating,
            a.HrComments, a.FinalRating,
            a.FinalReviewedBy, a.FinalReviewedAt
        ));
    }
}
