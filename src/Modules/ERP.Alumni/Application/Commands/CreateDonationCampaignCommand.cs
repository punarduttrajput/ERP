using ERP.Alumni.Domain;
using ERP.Alumni.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;

namespace ERP.Alumni.Application.Commands;

public record CreateDonationCampaignCommand(
    Guid TenantId,
    string Title,
    string? Description,
    decimal TargetAmount,
    DateOnly StartDate,
    DateOnly? EndDate,
    bool Section80GEligible,
    string? Section80GRegistrationNumber
) : IRequest<Result<Guid>>;

public class CreateDonationCampaignHandler : IRequestHandler<CreateDonationCampaignCommand, Result<Guid>>
{
    private readonly IAlumniDbContext _db;

    public CreateDonationCampaignHandler(IAlumniDbContext db) => _db = db;

    public async Task<Result<Guid>> Handle(CreateDonationCampaignCommand request, CancellationToken cancellationToken)
    {
        if (request.Section80GEligible && string.IsNullOrWhiteSpace(request.Section80GRegistrationNumber))
            return Result.Failure<Guid>("Section 80G registration number is required when the campaign is 80G eligible.");

        var campaign = new DonationCampaign
        {
            TenantId = request.TenantId,
            Title = request.Title,
            Description = request.Description,
            TargetAmount = request.TargetAmount,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            IsActive = true,
            Section80GEligible = request.Section80GEligible,
            Section80GRegistrationNumber = request.Section80GRegistrationNumber
        };

        _db.DonationCampaigns.Add(campaign);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(campaign.Id);
    }
}
