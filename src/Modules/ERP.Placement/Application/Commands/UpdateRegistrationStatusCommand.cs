using ERP.Placement.Domain;
using ERP.Placement.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Placement.Application.Commands;

public record UpdateRegistrationStatusCommand(
    Guid TenantId,
    Guid RegistrationId,
    RegistrationStatus NewStatus,
    string? Notes,
    DateTime? InterviewAt,
    decimal? OfferLpa
) : IRequest<Result>;

public class UpdateRegistrationStatusHandler : IRequestHandler<UpdateRegistrationStatusCommand, Result>
{
    private readonly IPlacementDbContext _db;

    public UpdateRegistrationStatusHandler(IPlacementDbContext db) => _db = db;

    public async Task<Result> Handle(UpdateRegistrationStatusCommand request, CancellationToken cancellationToken)
    {
        var registration = await _db.Registrations
            .Include(x => x.Drive)
            .FirstOrDefaultAsync(x => x.Id == request.RegistrationId, cancellationToken);
        if (registration is null)
            return Result.Failure("Registration not found.");

        if (!IsTransitionValid(registration.Status, request.NewStatus))
            return Result.Failure($"Invalid status transition from {registration.Status} to {request.NewStatus}.");

        registration.Status = request.NewStatus;

        if (request.NewStatus == RegistrationStatus.InterviewScheduled)
        {
            registration.InterviewScheduledAt = request.InterviewAt;
            registration.InterviewNotes = request.Notes;
        }

        if (request.NewStatus == RegistrationStatus.Selected)
        {
            if (!request.OfferLpa.HasValue)
                return Result.Failure("OfferLpa is required when selecting a student.");

            registration.OfferLpa = request.OfferLpa;

            var drive = registration.Drive!;

            var offer = new PlacementOffer
            {
                TenantId = request.TenantId,
                RegistrationId = registration.Id,
                DriveId = registration.DriveId,
                StudentId = registration.StudentId,
                CompanyName = drive.CompanyName,
                JobRole = drive.JobRole,
                OfferedPackageLpa = request.OfferLpa.Value,
                Status = OfferStatus.Issued,
                IssuedAt = DateTime.UtcNow
            };
            _db.Offers.Add(offer);

            drive.TotalSelected++;

            var company = await _db.Companies.FirstOrDefaultAsync(x => x.Id == drive.CompanyId, cancellationToken);
            if (company is not null)
            {
                company.TotalOffers++;
                if (request.OfferLpa.Value > company.HighestPackageLpa)
                    company.HighestPackageLpa = request.OfferLpa.Value;
                // Running average: newAvg = (oldAvg * (totalOffers - 1) + newPkg) / totalOffers
                company.AveragePackageLpa = (company.AveragePackageLpa * (company.TotalOffers - 1) + request.OfferLpa.Value) / company.TotalOffers;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private static bool IsTransitionValid(RegistrationStatus current, RegistrationStatus next) =>
        (current, next) switch
        {
            (RegistrationStatus.Registered, RegistrationStatus.Shortlisted) => true,
            (RegistrationStatus.Registered, RegistrationStatus.Rejected) => true,
            (RegistrationStatus.Shortlisted, RegistrationStatus.InterviewScheduled) => true,
            (RegistrationStatus.Shortlisted, RegistrationStatus.Rejected) => true,
            (RegistrationStatus.InterviewScheduled, RegistrationStatus.Selected) => true,
            (RegistrationStatus.InterviewScheduled, RegistrationStatus.Rejected) => true,
            // Withdrew is valid from any non-terminal state
            (RegistrationStatus.Registered, RegistrationStatus.Withdrew) => true,
            (RegistrationStatus.Shortlisted, RegistrationStatus.Withdrew) => true,
            (RegistrationStatus.InterviewScheduled, RegistrationStatus.Withdrew) => true,
            _ => false
        };
}
