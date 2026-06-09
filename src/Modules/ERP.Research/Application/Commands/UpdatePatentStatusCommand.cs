using ERP.Research.Domain;
using ERP.Research.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Research.Application.Commands;

public record UpdatePatentStatusCommand(
    Guid TenantId,
    Guid PatentId,
    PatentStatus NewStatus,
    string? GrantNumber,
    DateOnly? GrantDate) : IRequest<Result>;

public class UpdatePatentStatusHandler : IRequestHandler<UpdatePatentStatusCommand, Result>
{
    private readonly IResearchDbContext _db;

    private static readonly HashSet<(PatentStatus From, PatentStatus To)> _allowedTransitions = new()
    {
        (PatentStatus.Filed,           PatentStatus.UnderExamination),
        (PatentStatus.UnderExamination, PatentStatus.Granted),
        (PatentStatus.UnderExamination, PatentStatus.Rejected),
        (PatentStatus.Filed,           PatentStatus.Abandoned),
        (PatentStatus.UnderExamination, PatentStatus.Abandoned)
    };

    public UpdatePatentStatusHandler(IResearchDbContext db) => _db = db;

    public async Task<Result> Handle(UpdatePatentStatusCommand request, CancellationToken cancellationToken)
    {
        var patent = await _db.Patents
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == request.TenantId && x.Id == request.PatentId && !x.IsDeleted, cancellationToken);

        if (patent is null)
            return Result.Failure("Patent not found.");

        if (!_allowedTransitions.Contains((patent.Status, request.NewStatus)))
            return Result.Failure($"Transition from {patent.Status} to {request.NewStatus} is not allowed.");

        if (request.NewStatus == PatentStatus.Granted)
        {
            if (string.IsNullOrWhiteSpace(request.GrantNumber))
                return Result.Failure("Grant number is required when marking a patent as Granted.");

            if (!request.GrantDate.HasValue)
                return Result.Failure("Grant date is required when marking a patent as Granted.");

            patent.GrantNumber = request.GrantNumber;
            patent.GrantDate = request.GrantDate;
        }

        patent.Status = request.NewStatus;
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
