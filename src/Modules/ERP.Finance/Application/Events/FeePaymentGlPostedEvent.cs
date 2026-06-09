using MediatR;

namespace ERP.Finance.Application.Events;

public record FeePaymentGlPostedEvent(
    Guid TenantId,
    Guid StudentId,
    Guid PaymentId,
    string ReceiptNumber,
    decimal Amount,
    Guid JournalEntryId
) : INotification;
