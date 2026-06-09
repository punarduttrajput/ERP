using ERP.Admissions.Domain;
using ERP.Admissions.Infrastructure;
using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Admissions.Application.Commands;

public sealed class VerifyDocumentsHandler : IRequestHandler<VerifyDocumentsCommand, Result>
{
    private readonly IAdmissionsDbContext _db;
    private readonly ICurrentUser _currentUser;

    public VerifyDocumentsHandler(IAdmissionsDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(VerifyDocumentsCommand request, CancellationToken cancellationToken)
    {
        var app = await _db.Applications
            .Include(a => a.AuditEntries)
            .FirstOrDefaultAsync(a => a.Id == request.ApplicationId, cancellationToken);

        if (app is null)
            return Result.Failure("Application not found.");

        if (app.State != ApplicationState.Submitted && app.State != ApplicationState.UnderVerification)
            return Result.Failure($"Application is in {app.State} state and cannot be verified.");

        var actorId = _currentUser.UserId ?? Guid.Empty;

        if (app.State == ApplicationState.Submitted)
            app.Transition(ApplicationState.UnderVerification, actorId, "Verification started");

        var target = request.Approved ? ApplicationState.Verified : ApplicationState.Rejected;
        app.Transition(target, actorId, request.RejectionReason);

        if (!request.Approved)
            app.RejectionReason = request.RejectionReason;

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
