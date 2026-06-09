using ERP.Fees.Domain;
using ERP.Fees.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Fees.Application.Commands;

public record ProcessRefundCommand(Guid RefundId, string GatewayRefundId) : IRequest<Result>;

public class ProcessRefundHandler : IRequestHandler<ProcessRefundCommand, Result>
{
    private readonly IFeesDbContext _db;

    public ProcessRefundHandler(IFeesDbContext db)
    {
        _db = db;
    }

    public async Task<Result> Handle(ProcessRefundCommand request, CancellationToken cancellationToken)
    {
        var refund = await _db.RefundRequests.FirstOrDefaultAsync(r => r.Id == request.RefundId, cancellationToken);
        if (refund is null)
            return Result.Failure("Refund request not found.");

        if (refund.Status != RefundStatus.Approved)
            return Result.Failure("Refund is not in approved state.");

        refund.Status = RefundStatus.Processed;
        refund.GatewayRefundId = request.GatewayRefundId;
        refund.ProcessedAt = DateTime.UtcNow;

        var payment = await _db.FeePayments.FirstOrDefaultAsync(p => p.Id == refund.PaymentId, cancellationToken);
        if (payment is not null)
            payment.Status = PaymentStatus.Refunded;

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
