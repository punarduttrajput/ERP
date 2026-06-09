using ERP.Research.Domain;
using ERP.Research.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Research.Application.Queries;

public record PatentDto(
    Guid Id,
    string Title,
    string Inventors,
    string? ApplicationNumber,
    DateOnly? FilingDate,
    DateOnly? GrantDate,
    string? GrantNumber,
    PatentStatus Status,
    string PatentOffice,
    Guid? ResearchProjectId);

public record GetPatentsQuery(
    Guid TenantId,
    PatentStatus? Status,
    Guid? ProjectId,
    int Page,
    int PageSize) : IRequest<PagedResult<PatentDto>>;

public class GetPatentsHandler : IRequestHandler<GetPatentsQuery, PagedResult<PatentDto>>
{
    private readonly IResearchDbContext _db;

    public GetPatentsHandler(IResearchDbContext db) => _db = db;

    public async Task<PagedResult<PatentDto>> Handle(GetPatentsQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Patents
            .IgnoreQueryFilters()
            .Where(x => x.TenantId == request.TenantId && !x.IsDeleted);

        if (request.Status.HasValue)
            query = query.Where(x => x.Status == request.Status.Value);

        if (request.ProjectId.HasValue)
            query = query.Where(x => x.ResearchProjectId == request.ProjectId.Value);

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new PatentDto(
                x.Id, x.Title, x.Inventors, x.ApplicationNumber,
                x.FilingDate, x.GrantDate, x.GrantNumber,
                x.Status, x.PatentOffice, x.ResearchProjectId))
            .ToListAsync(cancellationToken);

        return new PagedResult<PatentDto>(items, total, request.Page, request.PageSize);
    }
}
