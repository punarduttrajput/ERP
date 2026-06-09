using ERP.Placement.Domain;
using ERP.Placement.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Placement.Application.Commands;

public record ConfirmOfferCommand(
    Guid OfferId,
    bool Accept
) : IRequest<Result>;

public class ConfirmOfferHandler : IRequestHandler<ConfirmOfferCommand, Result>
{
    private readonly IPlacementDbContext _db;

    public ConfirmOfferHandler(IPlacementDbContext db) => _db = db;

    public async Task<Result> Handle(ConfirmOfferCommand request, CancellationToken cancellationToken)
    {
        var offer = await _db.Offers
            .Include(x => x.Registration)
            .FirstOrDefaultAsync(x => x.Id == request.OfferId, cancellationToken);

        if (offer is null)
            return Result.Failure("Offer not found.");

        if (offer.Status != OfferStatus.Issued)
            return Result.Failure("Offer has already been actioned.");

        if (request.Accept)
        {
            offer.Status = OfferStatus.Accepted;
            offer.ConfirmedAt = DateTime.UtcNow;
            offer.Registration!.Status = RegistrationStatus.OfferConfirmed;
        }
        else
        {
            offer.Status = OfferStatus.Declined;
            offer.Registration!.Status = RegistrationStatus.Withdrew;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
