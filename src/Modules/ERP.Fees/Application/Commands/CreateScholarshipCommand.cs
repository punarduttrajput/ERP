using ERP.Fees.Domain;
using ERP.Fees.Infrastructure;
using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Common;
using MediatR;

namespace ERP.Fees.Application.Commands;

public record CreateScholarshipCommand(
    Guid TenantId,
    string Name,
    ScholarshipType ScholarshipType,
    decimal? DiscountAmount,
    decimal? DiscountPercent,
    decimal? MinMeritScore,
    string? EligibleCategories,
    int? MaxBeneficiaries
) : IRequest<Result<Guid>>;

public class CreateScholarshipHandler : IRequestHandler<CreateScholarshipCommand, Result<Guid>>
{
    private readonly IFeesDbContext _db;

    public CreateScholarshipHandler(IFeesDbContext db)
    {
        _db = db;
    }

    public async Task<Result<Guid>> Handle(CreateScholarshipCommand request, CancellationToken cancellationToken)
    {
        if (request.DiscountAmount is null && request.DiscountPercent is null)
            return Result<Guid>.Failure("Either DiscountAmount or DiscountPercent must be provided.");

        var scholarship = new Scholarship
        {
            TenantId = request.TenantId,
            Name = request.Name,
            ScholarshipType = request.ScholarshipType,
            DiscountAmount = request.DiscountAmount,
            DiscountPercent = request.DiscountPercent,
            MinMeritScore = request.MinMeritScore,
            EligibleCategories = request.EligibleCategories,
            MaxBeneficiaries = request.MaxBeneficiaries
        };

        _db.Scholarships.Add(scholarship);
        await _db.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(scholarship.Id);
    }
}
