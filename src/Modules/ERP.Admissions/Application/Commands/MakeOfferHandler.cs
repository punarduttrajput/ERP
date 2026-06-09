using ERP.Admissions.Domain;
using ERP.Admissions.Infrastructure;
using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERP.Admissions.Application.Commands;

public sealed class MakeOfferHandler : IRequestHandler<MakeOfferCommand, Result<int>>
{
    private readonly IAdmissionsDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<MakeOfferHandler> _logger;

    public MakeOfferHandler(IAdmissionsDbContext db, ICurrentUser currentUser, ILogger<MakeOfferHandler> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<Result<int>> Handle(MakeOfferCommand request, CancellationToken cancellationToken)
    {
        var seatMatrices = await _db.SeatMatrices
            .Where(s => s.ProgramId == request.ProgramId && s.AcademicYear == request.AcademicYear)
            .ToListAsync(cancellationToken);

        var rankedApps = await _db.Applications
            .Include(a => a.AuditEntries)
            .Where(a => a.ProgramId == request.ProgramId
                     && a.AcademicYear == request.AcademicYear
                     && a.State == ApplicationState.MeritEvaluated)
            .OrderBy(a => a.MeritRank)
            .ToListAsync(cancellationToken);

        if (!rankedApps.Any())
            return Result<int>.Success(0);

        var definition = await _db.WorkflowDefinitions
            .Where(w => w.IsActive)
            .OrderByDescending(w => w.Version)
            .FirstOrDefaultAsync(cancellationToken);

        var offerDays = definition?.OfferValidityDays ?? 7;
        var actorId   = _currentUser.UserId ?? Guid.Empty;
        var offerCount = 0;

        var seatsByCat = seatMatrices.ToDictionary(s => s.Category, s => s.AvailableSeats);

        foreach (var app in rankedApps)
        {
            if (!seatsByCat.TryGetValue(app.Category, out var available) || available <= 0)
            {
                app.Transition(ApplicationState.Rejected, actorId, "No seats available in category");
                app.RejectionReason = "No seats available in category";
                continue;
            }

            app.Transition(ApplicationState.OfferMade, actorId, "Offer issued");
            app.OfferExpiresAt = DateTime.UtcNow.AddDays(offerDays);
            seatsByCat[app.Category]--;
            offerCount++;
        }

        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("{Count} offers issued for program {ProgramId}", offerCount, request.ProgramId);
        return Result<int>.Success(offerCount);
    }
}
