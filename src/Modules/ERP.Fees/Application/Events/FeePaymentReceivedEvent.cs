using MediatR;

namespace ERP.Fees.Application.Events;

public record FeePaymentReceivedEvent(
    Guid TenantId,
    Guid StudentId,
    Guid PaymentId,
    decimal Amount,
    string ReceiptNumber,
    DateTime PaidAt
) : INotification;
