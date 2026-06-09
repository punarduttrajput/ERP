using ERP.Admissions.Domain;
using ERP.Admissions.Infrastructure;
using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Admissions.Application.Commands;

public sealed class WithdrawApplicationHandler : IRequestHandler<WithdrawApplicationCommand, Result>
{
    private readonly IAdmissionsDbContext _db;
    private readonly ICurrentUser _currentUser;

    public WithdrawApplicationHandler(IAdmissionsDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(WithdrawApplicationCommand request, CancellationToken cancellationToken)
    {
        var app = await _db.Applications
            .Include(a => a.AuditEntries)
            .FirstOrDefaultAsync(a => a.Id == request.ApplicationId, cancellationToken);

        if (app is null)
            return Result.Failure("Application not found.");

        if (!app.CanTransitionTo(ApplicationState.Withdrawn))
            return Result.Failure($"Application in {app.State} state cannot be withdrawn.");

        var actorId = _currentUser.UserId ?? Guid.Empty;
        app.Transition(ApplicationState.Withdrawn, actorId, "Withdrawn by applicant");

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
