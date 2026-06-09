using ERP.Accreditation.Infrastructure;
using ERP.Shared.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Accreditation.Application.Queries;

public record GetEvidenceTagsQuery(
    Guid TenantId,
    string? NaacCriterion,
    string? ModuleName,
    int Page,
    int PageSize
) : IRequest<PagedResult<EvidenceTagDto>>;

public record EvidenceTagDto(
    Guid Id,
    string ModuleName,
    string RecordId,
    string RecordLabel,
    string NaacCriterion,
    string NaacIndicator,
    Guid TaggedBy,
    string? Notes,
    DateTime CreatedAt
);

public record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize);

public class GetEvidenceTagsHandler : IRequestHandler<GetEvidenceTagsQuery, PagedResult<EvidenceTagDto>>
{
    private readonly IAccreditationDbContext _db;

    public GetEvidenceTagsHandler(IAccreditationDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<EvidenceTagDto>> Handle(GetEvidenceTagsQuery request, CancellationToken cancellationToken)
    {
        var query = _db.EvidenceTags
            .Where(t => t.TenantId == request.TenantId && !t.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.NaacCriterion))
            query = query.Where(t => t.NaacCriterion == request.NaacCriterion);

        if (!string.IsNullOrWhiteSpace(request.ModuleName))
            query = query.Where(t => t.ModuleName == request.ModuleName);

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(t => new EvidenceTagDto(
                t.Id,
                t.ModuleName,
                t.RecordId,
                t.RecordLabel,
                t.NaacCriterion,
                t.NaacIndicator,
                t.TaggedBy,
                t.Notes,
                t.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<EvidenceTagDto>(items, total, request.Page, request.PageSize);
    }
}
