using System.Security.Cryptography;
using System.Text;
using ERP.Fees.Application.Events;
using ERP.Fees.Domain;
using ERP.Fees.Infrastructure;
using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ERP.Fees.Application.Commands;

public record ConfirmPaymentCommand(
    string GatewayOrderId,
    string GatewayPaymentId,
    string GatewaySignature,
    decimal Amount
) : IRequest<Result>;

public class ConfirmPaymentHandler : IRequestHandler<ConfirmPaymentCommand, Result>
{
    private readonly IFeesDbContext _db;
    private readonly ICurrentTenant _currentTenant;
    private readonly IConfiguration _configuration;
    private readonly IPublisher _publisher;

    public ConfirmPaymentHandler(IFeesDbContext db, ICurrentTenant currentTenant, IConfiguration configuration, IPublisher publisher)
    {
        _db = db;
        _currentTenant = currentTenant;
        _configuration = configuration;
        _publisher = publisher;
    }

    public async Task<Result> Handle(ConfirmPaymentCommand request, CancellationToken cancellationToken)
    {
        var gatewaySecret = _configuration["PaymentGateway:Secret"] ?? string.Empty;
        var expectedSignature = Convert.ToHexString(
            HMACSHA256.HashData(
                Encoding.UTF8.GetBytes(gatewaySecret),
                Encoding.UTF8.GetBytes($"{request.GatewayOrderId}|{request.GatewayPaymentId}")));

        if (!expectedSignature.Equals(request.GatewaySignature, StringComparison.OrdinalIgnoreCase))
            return Result.Failure("Invalid payment signature.");

        var payment = await _db.FeePayments
            .Include(p => p.Account)
            .ThenInclude(a => a!.Installments)
            .FirstOrDefaultAsync(p => p.GatewayOrderId == request.GatewayOrderId, cancellationToken);

        if (payment is null)
            return Result.Failure("Payment record not found.");

        if (payment.Status == PaymentStatus.Paid)
            return Result.Success();

        var tenantId = _currentTenant.TenantId ?? Guid.Empty;
        var now = DateTime.UtcNow;

        payment.Status = PaymentStatus.Paid;
        payment.GatewayPaymentId = request.GatewayPaymentId;
        payment.GatewaySignature = request.GatewaySignature;
        payment.PaidAt = now;
        payment.ReceiptNumber = $"RCP-{tenantId.ToString()[..6]}-{now:yyyyMMdd}-{payment.Id.ToString()[..6]}".ToUpper();

        if (payment.InstallmentId.HasValue && payment.Account is not null)
        {
            var installment = payment.Account.Installments.FirstOrDefault(i => i.Id == payment.InstallmentId.Value);
            if (installment is not null)
            {
                installment.IsPaid = true;
                installment.PaidAt = now;
                installment.PaidAmount = payment.Amount;
            }
        }

        if (payment.Account is not null)
        {
            payment.Account.PaidAmount += request.Amount;
            payment.Account.DueAmount = payment.Account.NetAmount - payment.Account.PaidAmount;
            if (payment.Account.DueAmount <= 0)
                payment.Account.IsFullyPaid = true;
        }

        await _db.SaveChangesAsync(cancellationToken);

        var account = payment.Account;
        if (account is not null)
        {
            await _publisher.Publish(new FeePaymentReceivedEvent(
                tenantId,
                account.StudentId,
                payment.Id,
                request.Amount,
                payment.ReceiptNumber,
                now), cancellationToken);
        }

        return Result.Success();
    }
}
