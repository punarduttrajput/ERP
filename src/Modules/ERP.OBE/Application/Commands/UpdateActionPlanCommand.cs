using ERP.OBE.Domain;
using ERP.OBE.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.OBE.Application.Commands;

public record UpdateActionPlanCommand(
    Guid TenantId,
    Guid ActionPlanId,
    ActionPlanStatus Status,
    string? Outcome,
    Guid? AssignedTo,
    DateOnly? TargetDate) : IRequest<Result>;

public class UpdateActionPlanHandler : IRequestHandler<UpdateActionPlanCommand, Result>
{
    private readonly IObeDbContext _db;

    public UpdateActionPlanHandler(IObeDbContext db) => _db = db;

    public async Task<Result> Handle(UpdateActionPlanCommand request, CancellationToken cancellationToken)
    {
        var plan = await _db.ActionPlans.FirstOrDefaultAsync(
            x => x.TenantId == request.TenantId && x.Id == request.ActionPlanId,
            cancellationToken);

        if (plan is null)
            return Result.Failure("Action plan not found.");

        plan.Status = request.Status;
        if (request.Outcome is not null) plan.Outcome = request.Outcome;
        if (request.AssignedTo.HasValue) plan.AssignedTo = request.AssignedTo;
        if (request.TargetDate.HasValue) plan.TargetDate = request.TargetDate;

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
