using ERP.Alumni.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Alumni.Application.Queries;

public record DonationCampaignDto(
    Guid Id,
    string Title,
    string? Description,
    decimal TargetAmount,
    decimal CollectedAmount,
    DateOnly StartDate,
    DateOnly? EndDate,
    bool IsActive,
    bool Section80GEligible,
    string? Section80GRegistrationNumber
);

public record GetDonationCampaignQuery(Guid TenantId, Guid CampaignId) : IRequest<Result<DonationCampaignDto>>;

public class GetDonationCampaignHandler : IRequestHandler<GetDonationCampaignQuery, Result<DonationCampaignDto>>
{
    private readonly IAlumniDbContext _db;

    public GetDonationCampaignHandler(IAlumniDbContext db) => _db = db;

    public async Task<Result<DonationCampaignDto>> Handle(GetDonationCampaignQuery request, CancellationToken cancellationToken)
    {
        var campaign = await _db.DonationCampaigns
            .FirstOrDefaultAsync(x => x.Id == request.CampaignId && x.TenantId == request.TenantId, cancellationToken);

        if (campaign is null)
            return Result.Failure<DonationCampaignDto>("Donation campaign not found.");

        var dto = new DonationCampaignDto(
            campaign.Id,
            campaign.Title,
            campaign.Description,
            campaign.TargetAmount,
            campaign.CollectedAmount,
            campaign.StartDate,
            campaign.EndDate,
            campaign.IsActive,
            campaign.Section80GEligible,
            campaign.Section80GRegistrationNumber
        );

        return Result.Success(dto);
    }
}
