using System.Security.Cryptography;
using System.Text;
using ERP.Fees.Application.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace ERP.Fees.API;

[ApiController]
[Route("api/fees/webhook")]
public class PaymentWebhookController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IConfiguration _configuration;

    public PaymentWebhookController(IMediator mediator, IConfiguration configuration)
    {
        _mediator = mediator;
        _configuration = configuration;
    }

    [HttpPost("razorpay")]
    public async Task<IActionResult> RazorpayWebhook(CancellationToken ct)
    {
        using var reader = new StreamReader(HttpContext.Request.Body, Encoding.UTF8);
        var rawBody = await reader.ReadToEndAsync(ct);

        var webhookSecret = _configuration["PaymentGateway:RazorpayWebhookSecret"] ?? string.Empty;
        var signature = HttpContext.Request.Headers["X-Razorpay-Signature"].FirstOrDefault() ?? string.Empty;

        var expectedSignature = Convert.ToHexString(
            HMACSHA256.HashData(
                Encoding.UTF8.GetBytes(webhookSecret),
                Encoding.UTF8.GetBytes(rawBody)));

        if (!expectedSignature.Equals(signature, StringComparison.OrdinalIgnoreCase))
            return Ok(); // Always 200; return early but don't process invalid webhook

        var payload = System.Text.Json.JsonSerializer.Deserialize<RazorpayWebhookPayload>(rawBody,
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (payload?.Event == "payment.captured" && payload.Payload?.Payment?.Entity is not null)
        {
            var entity = payload.Payload.Payment.Entity;
            await _mediator.Send(new ConfirmPaymentCommand(
                entity.OrderId,
                entity.Id,
                entity.Signature ?? string.Empty,
                entity.Amount / 100m // Razorpay amounts are in paise
            ), ct);
        }

        return Ok();
    }

    [HttpPost("payu")]
    public async Task<IActionResult> PayUWebhook(CancellationToken ct)
    {
        // PayU sends txnid (maps to our orderId), mihpayid (their payment id), hash, and amount as form fields
        var form = HttpContext.Request.Form;
        var txnid = form["txnid"].FirstOrDefault() ?? string.Empty;
        var mihpayid = form["mihpayid"].FirstOrDefault() ?? string.Empty;
        var hash = form["hash"].FirstOrDefault() ?? string.Empty;
        var amountStr = form["amount"].FirstOrDefault() ?? "0";

        if (!string.IsNullOrEmpty(txnid) && !string.IsNullOrEmpty(mihpayid))
        {
            await _mediator.Send(new ConfirmPaymentCommand(
                txnid,
                mihpayid,
                hash,
                decimal.TryParse(amountStr, out var amount) ? amount : 0
            ), ct);
        }

        return Ok();
    }
}

internal record RazorpayWebhookPayload(
    string? Event,
    RazorpayWebhookData? Payload
);

internal record RazorpayWebhookData(RazorpayPaymentWrapper? Payment);

internal record RazorpayPaymentWrapper(RazorpayPaymentEntity? Entity);

internal record RazorpayPaymentEntity(
    string Id,
    string OrderId,
    string? Signature,
    long Amount
);
