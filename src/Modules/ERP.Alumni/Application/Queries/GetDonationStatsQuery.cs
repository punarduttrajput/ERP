using ERP.Alumni.Domain;
using ERP.Alumni.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Alumni.Application.Queries;

public record DonationStatsDto(
    Guid CampaignId,
    string Title,
    decimal TargetAmount,
    decimal CollectedAmount,
    decimal CollectionPercent,
    int TotalPledges,
    int FullyPaidPledges,
    int PartialPledges
);

public record GetDonationStatsQuery(Guid TenantId, Guid CampaignId) : IRequest<Result<DonationStatsDto>>;

public class GetDonationStatsHandler : IRequestHandler<GetDonationStatsQuery, Result<DonationStatsDto>>
{
    private readonly IAlumniDbContext _db;

    public GetDonationStatsHandler(IAlumniDbContext db) => _db = db;

    public async Task<Result<DonationStatsDto>> Handle(GetDonationStatsQuery request, CancellationToken cancellationToken)
    {
        var campaign = await _db.DonationCampaigns
            .FirstOrDefaultAsync(x => x.Id == request.CampaignId && x.TenantId == request.TenantId, cancellationToken);

        if (campaign is null)
            return Result.Failure<DonationStatsDto>("Donation campaign not found.");

        var pledges = await _db.DonationPledges
            .Where(x => x.CampaignId == request.CampaignId && x.TenantId == request.TenantId)
            .ToListAsync(cancellationToken);

        var total = pledges.Count;
        var fullyPaid = pledges.Count(x => x.Status == PledgeStatus.FullyPaid);
        var partial = pledges.Count(x => x.Status == PledgeStatus.PartiallyPaid);
        var percent = campaign.TargetAmount > 0
            ? Math.Round(campaign.CollectedAmount / campaign.TargetAmount * 100, 2)
            : 0;

        return Result.Success(new DonationStatsDto(
            campaign.Id,
            campaign.Title,
            campaign.TargetAmount,
            campaign.CollectedAmount,
            percent,
            total,
            fullyPaid,
            partial
        ));
    }
}
