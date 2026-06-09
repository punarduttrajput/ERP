using ERP.Alumni.Domain;
using ERP.Alumni.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Alumni.Application.Commands;

public record RecordDonationPaymentCommand(
    Guid TenantId,
    Guid PledgeId,
    decimal Amount
) : IRequest<Result>;

public class RecordDonationPaymentHandler : IRequestHandler<RecordDonationPaymentCommand, Result>
{
    private readonly IAlumniDbContext _db;

    public RecordDonationPaymentHandler(IAlumniDbContext db) => _db = db;

    public async Task<Result> Handle(RecordDonationPaymentCommand request, CancellationToken cancellationToken)
    {
        var pledge = await _db.DonationPledges
            .Include(x => x.Campaign)
            .FirstOrDefaultAsync(x => x.Id == request.PledgeId && x.TenantId == request.TenantId, cancellationToken);

        if (pledge is null)
            return Result.Failure("Pledge not found.");

        if (pledge.Status == PledgeStatus.FullyPaid)
            return Result.Failure("Pledge is already fully paid.");

        if (pledge.Status == PledgeStatus.Cancelled)
            return Result.Failure("Pledge has been cancelled.");

        pledge.PaidAmount += request.Amount;
        pledge.LastPaymentAt = DateTime.UtcNow;

        if (pledge.PaidAmount >= pledge.PledgedAmount)
        {
            pledge.Status = PledgeStatus.FullyPaid;
            pledge.ReceiptNumber = $"80G-{pledge.TenantId.ToString()[..4].ToUpper()}-{DateTime.UtcNow.Year}-{pledge.Id.ToString()[..6].ToUpper()}";
        }
        else if (pledge.PaidAmount > 0)
        {
            pledge.Status = PledgeStatus.PartiallyPaid;
        }

        if (pledge.Campaign is not null)
            pledge.Campaign.CollectedAmount += request.Amount;

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
