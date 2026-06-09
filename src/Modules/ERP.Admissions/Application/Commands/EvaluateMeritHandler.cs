using ERP.Admissions.Domain;
using ERP.Admissions.Infrastructure;
using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERP.Admissions.Application.Commands;

public sealed class EvaluateMeritHandler : IRequestHandler<EvaluateMeritCommand, Result<int>>
{
    private readonly IAdmissionsDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<EvaluateMeritHandler> _logger;

    public EvaluateMeritHandler(IAdmissionsDbContext db, ICurrentUser currentUser, ILogger<EvaluateMeritHandler> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<Result<int>> Handle(EvaluateMeritCommand request, CancellationToken cancellationToken)
    {
        var verified = await _db.Applications
            .Include(a => a.Documents)
            .Include(a => a.AuditEntries)
            .Where(a => a.ProgramId == request.ProgramId
                     && a.AcademicYear == request.AcademicYear
                     && a.State == ApplicationState.Verified)
            .ToListAsync(cancellationToken);

        if (!verified.Any())
            return Result<int>.Success(0);

        var ranked = verified
            .OrderByDescending(a => a.MeritScore ?? 0)
            .ToList();

        var actorId = _currentUser.UserId ?? Guid.Empty;

        for (var i = 0; i < ranked.Count; i++)
        {
            ranked[i].MeritRank = i + 1;
            ranked[i].Transition(ApplicationState.MeritEvaluated, actorId, $"Ranked #{i + 1}");
        }

        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Merit evaluated: {Count} applications ranked for program {ProgramId}", ranked.Count, request.ProgramId);
        return Result<int>.Success(ranked.Count);
    }
}
