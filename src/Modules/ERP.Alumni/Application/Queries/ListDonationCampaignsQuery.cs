using ERP.Alumni.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Alumni.Application.Queries;

public record ListDonationCampaignsQuery(Guid TenantId) : IRequest<Result<IReadOnlyList<DonationCampaignDto>>>;

public class ListDonationCampaignsHandler : IRequestHandler<ListDonationCampaignsQuery, Result<IReadOnlyList<DonationCampaignDto>>>
{
    private readonly IAlumniDbContext _db;

    public ListDonationCampaignsHandler(IAlumniDbContext db) => _db = db;

    public async Task<Result<IReadOnlyList<DonationCampaignDto>>> Handle(ListDonationCampaignsQuery request, CancellationToken cancellationToken)
    {
        var campaigns = await _db.DonationCampaigns
            .Where(x => x.TenantId == request.TenantId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new DonationCampaignDto(
                x.Id, x.Title, x.Description, x.TargetAmount, x.CollectedAmount,
                x.StartDate, x.EndDate, x.IsActive, x.Section80GEligible, x.Section80GRegistrationNumber))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<DonationCampaignDto>>(campaigns);
    }
}
