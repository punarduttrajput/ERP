using ERP.Research.Domain;
using ERP.Research.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Research.Application.Commands;

public record UpdatePublicationCommand(
    Guid TenantId,
    Guid PublicationId,
    string Title,
    PublicationType PublicationType,
    string VenueName,
    string? Isbn,
    string? IssueVolume,
    string? PageNumbers,
    int PublicationYear,
    string? Doi,
    decimal? ImpactFactor,
    PublicationIndex Index,
    bool IsUgcListed,
    Guid? ResearchProjectId) : IRequest<Result>;

public class UpdatePublicationHandler : IRequestHandler<UpdatePublicationCommand, Result>
{
    private readonly IResearchDbContext _db;

    public UpdatePublicationHandler(IResearchDbContext db) => _db = db;

    public async Task<Result> Handle(UpdatePublicationCommand request, CancellationToken cancellationToken)
    {
        var currentYear = DateTime.UtcNow.Year;

        if (request.PublicationYear < 1900 || request.PublicationYear > currentYear + 1)
            return Result.Failure($"Publication year must be between 1900 and {currentYear + 1}.");

        if (request.ImpactFactor.HasValue && request.ImpactFactor.Value < 0)
            return Result.Failure("Impact factor cannot be negative.");

        var publication = await _db.Publications
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == request.TenantId && x.Id == request.PublicationId && !x.IsDeleted, cancellationToken);

        if (publication is null)
            return Result.Failure("Publication not found.");

        publication.Title = request.Title;
        publication.PublicationType = request.PublicationType;
        publication.VenueName = request.VenueName;
        publication.Isbn = request.Isbn;
        publication.IssueVolume = request.IssueVolume;
        publication.PageNumbers = request.PageNumbers;
        publication.PublicationYear = request.PublicationYear;
        publication.Doi = request.Doi;
        publication.ImpactFactor = request.ImpactFactor;
        publication.Index = request.Index;
        publication.IsUgcListed = request.IsUgcListed;
        publication.ResearchProjectId = request.ResearchProjectId;

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
