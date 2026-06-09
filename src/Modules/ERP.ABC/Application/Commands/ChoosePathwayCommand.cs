using ERP.ABC.Domain;
using ERP.ABC.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.ABC.Application.Commands;

public record ChoosePathwayCommand(Guid TenantId, Guid StudentId, PathwayType PathwayType)
    : IRequest<Result<Guid>>;

public class ChoosePathwayHandler : IRequestHandler<ChoosePathwayCommand, Result<Guid>>
{
    private static readonly Dictionary<PathwayType, int> RequiredCredits = new()
    {
        { PathwayType.Certificate, 40 },
        { PathwayType.Diploma,     80 },
        { PathwayType.Degree,     120 },
        { PathwayType.PgDiploma,   60 },
        { PathwayType.PgDegree,    90 }
    };

    private readonly IAbcDbContext _db;

    public ChoosePathwayHandler(IAbcDbContext db) => _db = db;

    public async Task<Result<Guid>> Handle(ChoosePathwayCommand request, CancellationToken cancellationToken)
    {
        var profile = await _db.StudentAbcProfiles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == request.TenantId && x.StudentId == request.StudentId && !x.IsDeleted, cancellationToken);

        if (profile is null)
            return Result<Guid>.Failure("Student ABC profile not found.");

        var effectiveCredits = profile.TotalCreditsEarned + profile.TotalCreditsTransferredIn;
        var required = RequiredCredits[request.PathwayType];

        if (effectiveCredits < required)
            return Result<Guid>.Failure($"Insufficient credits for {request.PathwayType}. Required: {required}, Effective: {effectiveCredits}.");

        var pathway = await _db.AcademicPathways
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == request.TenantId && x.StudentId == request.StudentId && x.PathwayType == request.PathwayType && !x.IsDeleted, cancellationToken);

        if (pathway is null)
        {
            pathway = new AcademicPathway
            {
                TenantId = request.TenantId,
                StudentId = request.StudentId,
                PathwayType = request.PathwayType,
                RequiredCredits = required,
                CreditsEarned = effectiveCredits,
                IsEligible = true,
                SelectedAt = DateTime.UtcNow,
                Status = "Requested"
            };
            await _db.AcademicPathways.AddAsync(pathway, cancellationToken);
        }
        else
        {
            pathway.CreditsEarned = effectiveCredits;
            pathway.IsEligible = true;
            pathway.SelectedAt = DateTime.UtcNow;
            pathway.Status = "Requested";
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(pathway.Id);
    }
}
