using ERP.Fees.Domain;
using ERP.Fees.Infrastructure;
using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Fees.Application.Commands;

public record InitiateRefundCommand(
    Guid StudentId,
    Guid PaymentId,
    decimal Amount,
    string Reason,
    Guid InitiatedBy
) : IRequest<Result<Guid>>;

public class InitiateRefundHandler : IRequestHandler<InitiateRefundCommand, Result<Guid>>
{
    private readonly IFeesDbContext _db;
    private readonly ICurrentTenant _currentTenant;

    public InitiateRefundHandler(IFeesDbContext db, ICurrentTenant currentTenant)
    {
        _db = db;
        _currentTenant = currentTenant;
    }

    public async Task<Result<Guid>> Handle(InitiateRefundCommand request, CancellationToken cancellationToken)
    {
        var payment = await _db.FeePayments.FirstOrDefaultAsync(p => p.Id == request.PaymentId, cancellationToken);
        if (payment is null || payment.Status != PaymentStatus.Paid)
            return Result<Guid>.Failure("Payment not found or not in paid state.");

        if (request.Amount > payment.Amount)
            return Result<Guid>.Failure("Refund amount cannot exceed payment amount.");

        var refund = new RefundRequest
        {
            TenantId = _currentTenant.TenantId ?? Guid.Empty,
            StudentId = request.StudentId,
            PaymentId = request.PaymentId,
            Amount = request.Amount,
            Reason = request.Reason,
            Status = RefundStatus.Initiated,
            InitiatedBy = request.InitiatedBy
        };

        _db.RefundRequests.Add(refund);
        await _db.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(refund.Id);
    }
}
