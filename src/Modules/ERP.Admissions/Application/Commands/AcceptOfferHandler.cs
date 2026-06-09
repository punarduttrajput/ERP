using ERP.Admissions.Application.Events;
using ERP.Admissions.Domain;
using ERP.Admissions.Infrastructure;
using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERP.Admissions.Application.Commands;

public sealed class AcceptOfferHandler : IRequestHandler<AcceptOfferCommand, Result>
{
    private readonly IAdmissionsDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IPublisher _publisher;
    private readonly ILogger<AcceptOfferHandler> _logger;

    public AcceptOfferHandler(
        IAdmissionsDbContext db, ICurrentUser currentUser,
        IPublisher publisher, ILogger<AcceptOfferHandler> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task<Result> Handle(AcceptOfferCommand request, CancellationToken cancellationToken)
    {
        var app = await _db.Applications
            .Include(a => a.AuditEntries)
            .FirstOrDefaultAsync(a => a.Id == request.ApplicationId, cancellationToken);

        if (app is null)
            return Result.Failure("Application not found.");

        if (app.State != ApplicationState.OfferMade)
            return Result.Failure($"Cannot accept — application is in {app.State} state.");

        if (app.OfferExpiresAt.HasValue && DateTime.UtcNow > app.OfferExpiresAt.Value)
        {
            app.Transition(ApplicationState.Rejected, Guid.Empty, "Offer acceptance window expired");
            app.RejectionReason = "Offer expired";
            await _db.SaveChangesAsync(cancellationToken);
            return Result.Failure("Offer has expired.");
        }

        var actorId = _currentUser.UserId ?? Guid.Empty;
        app.Transition(ApplicationState.OfferAccepted, actorId, "Accepted by applicant");
        app.Transition(ApplicationState.Enrolled, actorId, "Enrollment confirmed");
        app.EnrolledAt = DateTime.UtcNow;

        var seat = await _db.SeatMatrices.FirstOrDefaultAsync(
            s => s.ProgramId == app.ProgramId && s.AcademicYear == app.AcademicYear && s.Category == app.Category,
            cancellationToken);
        if (seat is not null) seat.FilledSeats++;

        await _db.SaveChangesAsync(cancellationToken);

        await _publisher.Publish(new StudentEnrolledEvent(
            app.Id, app.TenantId, app.ApplicantName, app.ApplicantEmail,
            app.ApplicantMobile, app.ProgramId, app.ProgramName, app.AcademicYear), cancellationToken);

        _logger.LogInformation("Application {Id} enrolled", app.Id);
        return Result.Success();
    }
}
