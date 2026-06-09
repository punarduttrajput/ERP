using ERP.Fees.Application.Events;
using ERP.Finance.Application.Commands;
using ERP.Finance.Application.Events;
using ERP.Finance.Application.Queries;
using ERP.Shared.Application.Abstractions;
using MediatR;
using Microsoft.Extensions.Configuration;

namespace ERP.Finance.Application.Handlers;

public class FeePaymentReceivedEventHandler : INotificationHandler<FeePaymentReceivedEvent>
{
    private readonly IMediator _mediator;
    private readonly ICurrentUser? _currentUser;
    private readonly string _receivableAccountCode;
    private readonly string _cashAccountCode;

    public FeePaymentReceivedEventHandler(IMediator mediator, IConfiguration configuration, ICurrentUser? currentUser = null)
    {
        _mediator = mediator;
        _currentUser = currentUser;
        _receivableAccountCode = configuration["Finance:FeeReceivableAccountCode"] ?? "1100";
        _cashAccountCode = configuration["Finance:CashAccountCode"] ?? "1010";
    }

    public async Task Handle(FeePaymentReceivedEvent notification, CancellationToken cancellationToken)
    {
        var receivableResult = await FindAccountByCodeAsync(notification.TenantId, _receivableAccountCode, cancellationToken);
        var cashResult = await FindAccountByCodeAsync(notification.TenantId, _cashAccountCode, cancellationToken);

        if (receivableResult is null || cashResult is null)
            return;

        var lines = new List<JournalLineInput>
        {
            new(receivableResult.Value, notification.Amount, 0m, $"Fee receivable — Receipt {notification.ReceiptNumber}"),
            new(cashResult.Value, 0m, notification.Amount, $"Cash receipt — Receipt {notification.ReceiptNumber}")
        };

        var createResult = await _mediator.Send(new CreateJournalEntryCommand(
            notification.TenantId,
            DateOnly.FromDateTime(notification.PaidAt),
            $"Fee payment — Receipt {notification.ReceiptNumber} — Student {notification.StudentId}",
            notification.ReceiptNumber,
            lines
        ), cancellationToken);

        if (!createResult.IsSuccess)
            return;

        var postResult = await _mediator.Send(new PostJournalEntryCommand(
            notification.TenantId,
            createResult.Value,
            _currentUser?.UserId
        ), cancellationToken);

        if (!postResult.IsSuccess)
            return;

        await _mediator.Publish(new FeePaymentGlPostedEvent(
            notification.TenantId,
            notification.StudentId,
            notification.PaymentId,
            notification.ReceiptNumber,
            notification.Amount,
            createResult.Value
        ), cancellationToken);
    }

    private async Task<Guid?> FindAccountByCodeAsync(Guid tenantId, string code, CancellationToken cancellationToken)
    {
        // Dispatch a lightweight query through a direct db lookup via IFinanceDbContext.
        // We use a dedicated query to avoid coupling this handler to the db context directly.
        var result = await _mediator.Send(new FindAccountByCodeQuery(tenantId, code), cancellationToken);
        return result.IsSuccess ? result.Value : null;
    }
}
