using ERP.Fees.Domain;
using ERP.Fees.Infrastructure;
using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Fees.Application.Queries;

public record GetReceiptQuery(Guid PaymentId) : IRequest<Result<byte[]>>;

public class GetReceiptHandler : IRequestHandler<GetReceiptQuery, Result<byte[]>>
{
    private readonly IFeesDbContext _db;
    private readonly IPdfService _pdfService;

    public GetReceiptHandler(IFeesDbContext db, IPdfService pdfService)
    {
        _db = db;
        _pdfService = pdfService;
    }

    public async Task<Result<byte[]>> Handle(GetReceiptQuery request, CancellationToken cancellationToken)
    {
        var payment = await _db.FeePayments
            .Include(p => p.Account)
            .FirstOrDefaultAsync(p => p.Id == request.PaymentId, cancellationToken);

        if (payment is null)
            return Result<byte[]>.Failure("Payment not found.");

        if (payment.Status != PaymentStatus.Paid)
            return Result<byte[]>.Failure("Receipt available only for paid payments.");

        var account = payment.Account;
        var html = BuildReceiptHtml(payment, account);
        var bytes = await _pdfService.GeneratePdfAsync(html, cancellationToken);
        return Result<byte[]>.Success(bytes);
    }

    private static string BuildReceiptHtml(FeePayment payment, StudentFeeAccount? account)
    {
        var paidAt = payment.PaidAt?.ToString("dd MMM yyyy HH:mm UTC") ?? "-";
        var semester = account?.SemesterNumber.ToString() ?? "-";
        var academicYear = account?.AcademicYear.ToString() ?? "-";

        return $"""
            <!DOCTYPE html>
            <html>
            <head>
            <meta charset="utf-8"/>
            <title>Fee Receipt</title>
            </head>
            <body style="font-family: Arial, sans-serif; margin: 40px; color: #333;">
              <div style="border: 2px solid #2c3e50; padding: 30px; max-width: 700px; margin: 0 auto;">
                <h1 style="text-align: center; color: #2c3e50; margin-bottom: 5px;">Fee Payment Receipt</h1>
                <hr style="border-color: #2c3e50;"/>
                <table style="width: 100%; margin-top: 20px;">
                  <tr>
                    <td style="padding: 8px 0; font-weight: bold;">Receipt Number:</td>
                    <td style="padding: 8px 0;">{payment.ReceiptNumber}</td>
                    <td style="padding: 8px 0; font-weight: bold;">Date:</td>
                    <td style="padding: 8px 0;">{paidAt}</td>
                  </tr>
                  <tr>
                    <td style="padding: 8px 0; font-weight: bold;">Student ID:</td>
                    <td style="padding: 8px 0;">{account?.StudentId}</td>
                    <td style="padding: 8px 0; font-weight: bold;">Semester:</td>
                    <td style="padding: 8px 0;">{semester}</td>
                  </tr>
                  <tr>
                    <td style="padding: 8px 0; font-weight: bold;">Academic Year:</td>
                    <td style="padding: 8px 0;">{academicYear}</td>
                    <td style="padding: 8px 0; font-weight: bold;">Payment Method:</td>
                    <td style="padding: 8px 0;">{payment.PaymentMethod ?? "-"}</td>
                  </tr>
                </table>
                <hr style="margin-top: 20px;"/>
                <table style="width: 100%; margin-top: 20px;">
                  <tr style="background-color: #2c3e50; color: white;">
                    <th style="padding: 10px; text-align: left;">Description</th>
                    <th style="padding: 10px; text-align: right;">Amount</th>
                  </tr>
                  <tr>
                    <td style="padding: 10px; border-bottom: 1px solid #eee;">Fee Payment</td>
                    <td style="padding: 10px; text-align: right; border-bottom: 1px solid #eee;">&#8377; {payment.Amount:N2}</td>
                  </tr>
                  <tr style="font-weight: bold;">
                    <td style="padding: 10px;">Total Paid</td>
                    <td style="padding: 10px; text-align: right;">&#8377; {payment.Amount:N2}</td>
                  </tr>
                </table>
                <hr style="margin-top: 20px;"/>
                <p style="font-size: 12px; color: #666; margin-top: 20px;">
                  Gateway Reference: {payment.GatewayPaymentId ?? "-"}<br/>
                  Order ID: {payment.GatewayOrderId}
                </p>
                <p style="font-size: 11px; color: #999; text-align: center; margin-top: 30px;">
                  This is a system-generated receipt and does not require a physical signature.
                </p>
              </div>
            </body>
            </html>
            """;
    }
}
