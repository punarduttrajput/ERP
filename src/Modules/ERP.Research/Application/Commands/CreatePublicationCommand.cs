using ERP.Research.Domain;
using ERP.Research.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;

namespace ERP.Research.Application.Commands;

public record CreatePublicationCommand(
    Guid TenantId,
    Guid FacultyId,
    string FacultyName,
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
    Guid? ResearchProjectId) : IRequest<Result<Guid>>;

public class CreatePublicationHandler : IRequestHandler<CreatePublicationCommand, Result<Guid>>
{
    private readonly IResearchDbContext _db;

    public CreatePublicationHandler(IResearchDbContext db) => _db = db;

    public async Task<Result<Guid>> Handle(CreatePublicationCommand request, CancellationToken cancellationToken)
    {
        var currentYear = DateTime.UtcNow.Year;

        if (request.PublicationYear < 1900 || request.PublicationYear > currentYear + 1)
            return Result<Guid>.Failure($"Publication year must be between 1900 and {currentYear + 1}.");

        if (request.ImpactFactor.HasValue && request.ImpactFactor.Value < 0)
            return Result<Guid>.Failure("Impact factor cannot be negative.");

        var publication = new Publication
        {
            TenantId = request.TenantId,
            FacultyId = request.FacultyId,
            FacultyName = request.FacultyName,
            Title = request.Title,
            PublicationType = request.PublicationType,
            VenueName = request.VenueName,
            Isbn = request.Isbn,
            IssueVolume = request.IssueVolume,
            PageNumbers = request.PageNumbers,
            PublicationYear = request.PublicationYear,
            Doi = request.Doi,
            ImpactFactor = request.ImpactFactor,
            Index = request.Index,
            IsUgcListed = request.IsUgcListed,
            ResearchProjectId = request.ResearchProjectId
        };

        await _db.Publications.AddAsync(publication, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(publication.Id);
    }
}
