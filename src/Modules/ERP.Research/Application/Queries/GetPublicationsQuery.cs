using ERP.Research.Domain;
using ERP.Research.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Research.Application.Queries;

public record PublicationDto(
    Guid Id,
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
    Guid? ResearchProjectId);

public record GetPublicationsQuery(
    Guid TenantId,
    Guid? FacultyId,
    PublicationType? Type,
    int? Year,
    PublicationIndex? Index,
    int Page,
    int PageSize) : IRequest<PagedResult<PublicationDto>>;

public class GetPublicationsHandler : IRequestHandler<GetPublicationsQuery, PagedResult<PublicationDto>>
{
    private readonly IResearchDbContext _db;

    public GetPublicationsHandler(IResearchDbContext db) => _db = db;

    public async Task<PagedResult<PublicationDto>> Handle(GetPublicationsQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Publications
            .IgnoreQueryFilters()
            .Where(x => x.TenantId == request.TenantId && !x.IsDeleted);

        if (request.FacultyId.HasValue)
            query = query.Where(x => x.FacultyId == request.FacultyId.Value);

        if (request.Type.HasValue)
            query = query.Where(x => x.PublicationType == request.Type.Value);

        if (request.Year.HasValue)
            query = query.Where(x => x.PublicationYear == request.Year.Value);

        if (request.Index.HasValue)
            query = query.Where(x => x.Index == request.Index.Value);

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.PublicationYear)
            .ThenByDescending(x => x.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new PublicationDto(
                x.Id, x.FacultyId, x.FacultyName, x.Title, x.PublicationType,
                x.VenueName, x.Isbn, x.IssueVolume, x.PageNumbers, x.PublicationYear,
                x.Doi, x.ImpactFactor, x.Index, x.IsUgcListed, x.ResearchProjectId))
            .ToListAsync(cancellationToken);

        return new PagedResult<PublicationDto>(items, total, request.Page, request.PageSize);
    }
}
