using ERP.HRMS.Application.Events;
using ERP.HRMS.Domain;
using ERP.HRMS.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.HRMS.Application.Commands;

public record AdvanceRecruitmentCommand(
    Guid ApplicationId,
    Guid TenantId,
    RecruitmentStatus TargetStatus,
    string? Notes,
    DateTime? InterviewDate,
    decimal? OfferSalary
) : IRequest<Result>;

public class AdvanceRecruitmentHandler : IRequestHandler<AdvanceRecruitmentCommand, Result>
{
    private readonly IHrmsDbContext _db;
    private readonly IMediator _mediator;

    public AdvanceRecruitmentHandler(IHrmsDbContext db, IMediator mediator)
    {
        _db = db;
        _mediator = mediator;
    }

    public async Task<Result> Handle(AdvanceRecruitmentCommand request, CancellationToken cancellationToken)
    {
        var app = await _db.JobApplications
            .Include(a => a.Requisition)
            .FirstOrDefaultAsync(a => a.Id == request.ApplicationId && a.TenantId == request.TenantId, cancellationToken);

        if (app is null)
            return Result.Failure("Application not found.");

        var validTransition = (app.Status, request.TargetStatus) switch
        {
            (RecruitmentStatus.Applied, RecruitmentStatus.Shortlisted) => true,
            (RecruitmentStatus.Shortlisted, RecruitmentStatus.Interview) => true,
            (RecruitmentStatus.Interview, RecruitmentStatus.Offered) => true,
            (RecruitmentStatus.Offered, RecruitmentStatus.Joined) => true,
            (_, RecruitmentStatus.Rejected) when app.Status is not RecruitmentStatus.Joined and not RecruitmentStatus.Rejected and not RecruitmentStatus.Withdrawn => true,
            (_, RecruitmentStatus.Withdrawn) when app.Status is not RecruitmentStatus.Joined and not RecruitmentStatus.Rejected and not RecruitmentStatus.Withdrawn => true,
            _ => false
        };

        if (!validTransition)
            return Result.Failure($"Cannot transition from {app.Status} to {request.TargetStatus}.");

        app.Status = request.TargetStatus;

        if (request.TargetStatus == RecruitmentStatus.Interview)
        {
            app.InterviewDate = request.InterviewDate;
            app.InterviewNotes = request.Notes;
        }

        if (request.TargetStatus == RecruitmentStatus.Offered)
            app.OfferSalary = request.OfferSalary;

        if (request.TargetStatus is RecruitmentStatus.Rejected or RecruitmentStatus.Withdrawn)
            app.RejectionReason = request.Notes;

        await _db.SaveChangesAsync(cancellationToken);

        if (request.TargetStatus == RecruitmentStatus.Joined)
        {
            await _mediator.Publish(new EmployeeJoinedEvent(
                request.TenantId,
                app.Id,
                app.ApplicantEmail,
                app.ApplicantName,
                string.Empty,
                app.ApplicantMobile
            ), cancellationToken);
        }

        return Result.Success();
    }
}
