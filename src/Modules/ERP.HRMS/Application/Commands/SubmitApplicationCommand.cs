using ERP.HRMS.Domain;
using ERP.HRMS.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.HRMS.Application.Commands;

public record SubmitApplicationCommand(
    Guid TenantId,
    Guid RequisitionId,
    string ApplicantName,
    string ApplicantEmail,
    string? ApplicantMobile,
    string? ResumeBlobUrl
) : IRequest<Result<Guid>>;

public class SubmitApplicationHandler : IRequestHandler<SubmitApplicationCommand, Result<Guid>>
{
    private readonly IHrmsDbContext _db;

    public SubmitApplicationHandler(IHrmsDbContext db)
    {
        _db = db;
    }

    public async Task<Result<Guid>> Handle(SubmitApplicationCommand request, CancellationToken cancellationToken)
    {
        var req = await _db.RecruitmentRequisitions
            .FirstOrDefaultAsync(r => r.Id == request.RequisitionId && r.TenantId == request.TenantId && r.IsPublished, cancellationToken);

        if (req is null)
            return Result.Failure<Guid>("Job posting not found or not published.");

        var app = new JobApplication
        {
            TenantId = request.TenantId,
            RequisitionId = request.RequisitionId,
            ApplicantName = request.ApplicantName,
            ApplicantEmail = request.ApplicantEmail,
            ApplicantMobile = request.ApplicantMobile,
            ResumeBlobUrl = request.ResumeBlobUrl,
            Status = RecruitmentStatus.Applied
        };

        _db.JobApplications.Add(app);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(app.Id);
    }
}
