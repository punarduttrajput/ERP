using ERP.Fees.Domain;
using ERP.Fees.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Fees.Application.Commands;

public record ApproveRefundCommand(Guid RefundId, Guid ApprovedBy) : IRequest<Result>;

public class ApproveRefundHandler : IRequestHandler<ApproveRefundCommand, Result>
{
    private readonly IFeesDbContext _db;

    public ApproveRefundHandler(IFeesDbContext db)
    {
        _db = db;
    }

    public async Task<Result> Handle(ApproveRefundCommand request, CancellationToken cancellationToken)
    {
        var refund = await _db.RefundRequests.FirstOrDefaultAsync(r => r.Id == request.RefundId, cancellationToken);
        if (refund is null)
            return Result.Failure("Refund request not found.");

        if (refund.Status != RefundStatus.Initiated)
            return Result.Failure("Refund is not in initiated state.");

        refund.Status = RefundStatus.Approved;
        refund.ApprovedBy = request.ApprovedBy;
        refund.ApprovedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
