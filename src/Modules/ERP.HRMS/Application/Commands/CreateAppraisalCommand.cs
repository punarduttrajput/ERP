using ERP.HRMS.Domain;
using ERP.HRMS.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.HRMS.Application.Commands;

public record CreateAppraisalCommand(
    Guid TenantId,
    Guid EmployeeId,
    int ReviewYear
) : IRequest<Result<Guid>>;

public class CreateAppraisalHandler : IRequestHandler<CreateAppraisalCommand, Result<Guid>>
{
    private readonly IHrmsDbContext _db;

    public CreateAppraisalHandler(IHrmsDbContext db)
    {
        _db = db;
    }

    public async Task<Result<Guid>> Handle(CreateAppraisalCommand request, CancellationToken cancellationToken)
    {
        var exists = await _db.Appraisals
            .AnyAsync(a => a.TenantId == request.TenantId && a.EmployeeId == request.EmployeeId && a.ReviewYear == request.ReviewYear, cancellationToken);

        if (exists)
            return Result.Failure<Guid>($"Appraisal for employee already exists for year {request.ReviewYear}.");

        var appraisal = new Appraisal
        {
            TenantId = request.TenantId,
            EmployeeId = request.EmployeeId,
            ReviewYear = request.ReviewYear,
            Status = AppraisalStatus.SelfAssessmentPending
        };

        _db.Appraisals.Add(appraisal);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(appraisal.Id);
    }
}
