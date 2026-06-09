using ERP.Alumni.Domain;
using ERP.Alumni.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Alumni.Application.Commands;

public record PledgeDonationCommand(
    Guid TenantId,
    Guid CampaignId,
    Guid AlumniId,
    decimal Amount
) : IRequest<Result<Guid>>;

public class PledgeDonationHandler : IRequestHandler<PledgeDonationCommand, Result<Guid>>
{
    private readonly IAlumniDbContext _db;

    public PledgeDonationHandler(IAlumniDbContext db) => _db = db;

    public async Task<Result<Guid>> Handle(PledgeDonationCommand request, CancellationToken cancellationToken)
    {
        var campaign = await _db.DonationCampaigns
            .FirstOrDefaultAsync(x => x.Id == request.CampaignId && x.TenantId == request.TenantId, cancellationToken);

        if (campaign is null)
            return Result.Failure<Guid>("Donation campaign not found.");

        if (!campaign.IsActive)
            return Result.Failure<Guid>("Donation campaign is not active.");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (campaign.EndDate.HasValue && today > campaign.EndDate.Value)
            return Result.Failure<Guid>("Donation campaign has ended.");

        var alumni = await _db.AlumniProfiles
            .FirstOrDefaultAsync(x => x.Id == request.AlumniId && x.TenantId == request.TenantId, cancellationToken);

        if (alumni is null)
            return Result.Failure<Guid>("Alumni profile not found.");

        var pledge = new DonationPledge
        {
            TenantId = request.TenantId,
            CampaignId = request.CampaignId,
            AlumniId = request.AlumniId,
            AlumniName = $"{alumni.FirstName} {alumni.LastName}",
            AlumniEmail = alumni.Email,
            PledgedAmount = request.Amount,
            PaidAmount = 0,
            Status = PledgeStatus.Pledged,
            PledgedAt = DateTime.UtcNow
        };

        _db.DonationPledges.Add(pledge);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(pledge.Id);
    }
}
