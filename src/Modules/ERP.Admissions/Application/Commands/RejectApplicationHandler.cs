using ERP.Admissions.Domain;
using ERP.Admissions.Infrastructure;
using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Admissions.Application.Commands;

public sealed class RejectApplicationHandler : IRequestHandler<RejectApplicationCommand, Result>
{
    private readonly IAdmissionsDbContext _db;
    private readonly ICurrentUser _currentUser;

    public RejectApplicationHandler(IAdmissionsDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(RejectApplicationCommand request, CancellationToken cancellationToken)
    {
        var app = await _db.Applications
            .Include(a => a.AuditEntries)
            .FirstOrDefaultAsync(a => a.Id == request.ApplicationId, cancellationToken);

        if (app is null)
            return Result.Failure("Application not found.");

        if (!app.CanTransitionTo(ApplicationState.Rejected))
            return Result.Failure($"Cannot reject application in {app.State} state.");

        var actorId = _currentUser.UserId ?? Guid.Empty;
        app.Transition(ApplicationState.Rejected, actorId, request.Reason);
        app.RejectionReason = request.Reason;

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
