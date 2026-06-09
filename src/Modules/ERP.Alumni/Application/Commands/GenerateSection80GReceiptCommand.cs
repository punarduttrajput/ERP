using ERP.Alumni.Domain;
using ERP.Alumni.Infrastructure;
using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Alumni.Application.Commands;

public record GenerateSection80GReceiptCommand(
    Guid TenantId,
    Guid PledgeId
) : IRequest<Result<byte[]>>;

public class GenerateSection80GReceiptHandler : IRequestHandler<GenerateSection80GReceiptCommand, Result<byte[]>>
{
    private readonly IAlumniDbContext _db;
    private readonly IPdfService _pdfService;

    public GenerateSection80GReceiptHandler(IAlumniDbContext db, IPdfService pdfService)
    {
        _db = db;
        _pdfService = pdfService;
    }

    public async Task<Result<byte[]>> Handle(GenerateSection80GReceiptCommand request, CancellationToken cancellationToken)
    {
        var pledge = await _db.DonationPledges
            .Include(x => x.Campaign)
            .FirstOrDefaultAsync(x => x.Id == request.PledgeId && x.TenantId == request.TenantId, cancellationToken);

        if (pledge is null)
            return Result.Failure<byte[]>("Pledge not found.");

        if (pledge.Status != PledgeStatus.FullyPaid)
            return Result.Failure<byte[]>("Receipt can only be generated for fully paid pledges.");

        if (pledge.Campaign is null || !pledge.Campaign.Section80GEligible)
            return Result.Failure<byte[]>("Campaign is not eligible for Section 80G receipts.");

        var html = $"""
            <!DOCTYPE html>
            <html>
            <head><meta charset="utf-8" /><title>Section 80G Donation Receipt</title></head>
            <body style="font-family: Arial, sans-serif; margin: 40px;">
              <h2>Section 80G Donation Receipt</h2>
              <p><strong>Receipt No:</strong> {pledge.ReceiptNumber}</p>
              <p><strong>Date:</strong> {pledge.LastPaymentAt:dd-MMM-yyyy}</p>
              <p><strong>Received from:</strong> {pledge.AlumniName}</p>
              <p><strong>Amount:</strong> &#x20B9;{pledge.PaidAmount:N2}</p>
              <p><strong>Campaign:</strong> {pledge.Campaign.Title}</p>
              <p><strong>80G Registration No:</strong> {pledge.Campaign.Section80GRegistrationNumber}</p>
              <p>This donation qualifies for tax deduction under Section 80G of the Income Tax Act, 1961.</p>
            </body>
            </html>
            """;

        var bytes = await _pdfService.GeneratePdfAsync(html, cancellationToken);
        return Result.Success(bytes);
    }
}
