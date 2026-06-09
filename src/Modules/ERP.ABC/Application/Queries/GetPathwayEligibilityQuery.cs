using ERP.ABC.Domain;
using ERP.ABC.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.ABC.Application.Queries;

public record PathwayOption(
    PathwayType Type,
    string Label,
    int RequiredCredits,
    bool IsEligible,
    int CreditsShortfall);

public record PathwayEligibilityDto(
    int TotalCreditsEarned,
    int TransferredIn,
    int EffectiveCredits,
    IReadOnlyList<PathwayOption> EligiblePathways);

public record GetPathwayEligibilityQuery(Guid TenantId, Guid StudentId)
    : IRequest<Result<PathwayEligibilityDto>>;

public class GetPathwayEligibilityHandler : IRequestHandler<GetPathwayEligibilityQuery, Result<PathwayEligibilityDto>>
{
    private static readonly (PathwayType Type, string Label, int Required)[] Pathways =
    {
        (PathwayType.Certificate, "Certificate",  40),
        (PathwayType.Diploma,     "Diploma",      80),
        (PathwayType.Degree,      "Degree",       120),
        (PathwayType.PgDiploma,   "PG Diploma",   60),
        (PathwayType.PgDegree,    "PG Degree",    90)
    };

    private readonly IAbcDbContext _db;

    public GetPathwayEligibilityHandler(IAbcDbContext db) => _db = db;

    public async Task<Result<PathwayEligibilityDto>> Handle(GetPathwayEligibilityQuery request, CancellationToken cancellationToken)
    {
        var profile = await _db.StudentAbcProfiles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == request.TenantId && x.StudentId == request.StudentId && !x.IsDeleted, cancellationToken);

        if (profile is null)
            return Result<PathwayEligibilityDto>.Failure("ABC profile not found for this student.");

        var effective = profile.TotalCreditsEarned + profile.TotalCreditsTransferredIn;
        var options = Pathways.Select(p => new PathwayOption(
            p.Type,
            p.Label,
            p.Required,
            effective >= p.Required,
            effective >= p.Required ? 0 : p.Required - effective)).ToList();

        return Result<PathwayEligibilityDto>.Success(new PathwayEligibilityDto(
            profile.TotalCreditsEarned,
            profile.TotalCreditsTransferredIn,
            effective,
            options));
    }
}
