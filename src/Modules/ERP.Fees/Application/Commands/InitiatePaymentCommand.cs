using ERP.Fees.Application.Services;
using ERP.Fees.Domain;
using ERP.Fees.Infrastructure;
using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ERP.Fees.Application.Commands;

public record InitiatePaymentCommand(
    Guid AccountId,
    Guid? InstallmentId,
    decimal Amount,
    string? PaymentMethod
) : IRequest<Result<PaymentInitResponse>>;

public record PaymentInitResponse(string OrderId, decimal Amount, string GatewayKey);

public class InitiatePaymentHandler : IRequestHandler<InitiatePaymentCommand, Result<PaymentInitResponse>>
{
    private readonly IFeesDbContext _db;
    private readonly ICurrentTenant _currentTenant;
    private readonly IConfiguration _configuration;
    private readonly LateFineCalculatorService _lateFineCalculator;

    public InitiatePaymentHandler(IFeesDbContext db, ICurrentTenant currentTenant, IConfiguration configuration, LateFineCalculatorService lateFineCalculator)
    {
        _db = db;
        _currentTenant = currentTenant;
        _configuration = configuration;
        _lateFineCalculator = lateFineCalculator;
    }

    public async Task<Result<PaymentInitResponse>> Handle(InitiatePaymentCommand request, CancellationToken cancellationToken)
    {
        var account = await _db.StudentFeeAccounts
            .Include(a => a.Installments)
            .FirstOrDefaultAsync(a => a.Id == request.AccountId, cancellationToken);

        if (account is null)
            return Result<PaymentInitResponse>.Failure("Fee account not found.");

        if (account.DueAmount <= 0)
            return Result<PaymentInitResponse>.Failure("No outstanding dues.");

        if (request.InstallmentId.HasValue)
        {
            var installment = account.Installments.FirstOrDefault(i => i.Id == request.InstallmentId.Value);
            if (installment is null)
                return Result<PaymentInitResponse>.Failure("Installment not found.");

            var lateFine = _lateFineCalculator.Calculate(
                installment.DueDate,
                DateOnly.FromDateTime(DateTime.UtcNow),
                installment.BaseAmount > 0 ? await GetLateFinePerDay(installment.AccountId, installment.InstallmentNumber, cancellationToken) : 0,
                await GetMaxLateFine(installment.AccountId, installment.InstallmentNumber, cancellationToken));

            installment.LateFine = lateFine;
            installment.TotalDue = installment.BaseAmount + lateFine;
        }

        var tenantId = _currentTenant.TenantId ?? Guid.Empty;
        var orderId = $"ORD-{tenantId.ToString()[..8]}-{Guid.NewGuid().ToString()[..8]}".ToUpper();
        var gatewayKey = _configuration["PaymentGateway:RazorpayKeyId"] ?? string.Empty;

        var payment = new FeePayment
        {
            TenantId = tenantId,
            AccountId = request.AccountId,
            InstallmentId = request.InstallmentId,
            GatewayOrderId = orderId,
            Amount = request.Amount,
            Status = PaymentStatus.Initiated,
            PaymentMethod = request.PaymentMethod
        };

        _db.FeePayments.Add(payment);
        await _db.SaveChangesAsync(cancellationToken);

        return Result<PaymentInitResponse>.Success(new PaymentInitResponse(orderId, request.Amount, gatewayKey));
    }

    private async Task<decimal> GetLateFinePerDay(Guid accountId, int installmentNumber, CancellationToken ct)
    {
        var account = await _db.StudentFeeAccounts
            .Include(a => a.Installments)
            .FirstOrDefaultAsync(a => a.Id == accountId, ct);

        if (account is null) return 0;

        var schedule = await _db.InstallmentSchedules
            .Where(s => s.FeeStructureId == account.FeeStructureId && s.InstallmentNumber == installmentNumber)
            .FirstOrDefaultAsync(ct);

        return schedule?.LateFinePerDay ?? 0;
    }

    private async Task<decimal> GetMaxLateFine(Guid accountId, int installmentNumber, CancellationToken ct)
    {
        var account = await _db.StudentFeeAccounts
            .FirstOrDefaultAsync(a => a.Id == accountId, ct);

        if (account is null) return 0;

        var schedule = await _db.InstallmentSchedules
            .Where(s => s.FeeStructureId == account.FeeStructureId && s.InstallmentNumber == installmentNumber)
            .FirstOrDefaultAsync(ct);

        return schedule?.MaxLateFine ?? 0;
    }
}
