using ERP.Research.Domain;
using ERP.Research.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Research.Application.Queries;

public record GrantDto(
    Guid Id,
    string Title,
    string FundingAgency,
    string? GrantNumber,
    decimal SanctionedAmount,
    decimal DisbursedAmount,
    decimal UtilizedAmount,
    DateOnly? StartDate,
    DateOnly? EndDate,
    GrantStatus Status,
    Guid PrincipalInvestigatorId,
    Guid? ResearchProjectId,
    IReadOnlyList<GrantDisbursementDto> Disbursements);

public record GrantDisbursementDto(
    Guid Id,
    decimal Amount,
    DateOnly DisbursedAt,
    string? Reference,
    string? Notes);

public record GetGrantsQuery(
    Guid TenantId,
    GrantStatus? Status,
    Guid? PiId,
    bool IncludeDisbursements,
    int Page,
    int PageSize) : IRequest<PagedResult<GrantDto>>;

public class GetGrantsHandler : IRequestHandler<GetGrantsQuery, PagedResult<GrantDto>>
{
    private readonly IResearchDbContext _db;

    public GetGrantsHandler(IResearchDbContext db) => _db = db;

    public async Task<PagedResult<GrantDto>> Handle(GetGrantsQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Grants
            .IgnoreQueryFilters()
            .Include(x => x.Disbursements)
            .Where(x => x.TenantId == request.TenantId && !x.IsDeleted);

        if (request.Status.HasValue)
            query = query.Where(x => x.Status == request.Status.Value);

        if (request.PiId.HasValue)
            query = query.Where(x => x.PrincipalInvestigatorId == request.PiId.Value);

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new GrantDto(
                x.Id, x.Title, x.FundingAgency, x.GrantNumber,
                x.SanctionedAmount, x.DisbursedAmount, x.UtilizedAmount,
                x.StartDate, x.EndDate, x.Status, x.PrincipalInvestigatorId, x.ResearchProjectId,
                x.Disbursements.Where(d => !d.IsDeleted)
                    .Select(d => new GrantDisbursementDto(d.Id, d.Amount, d.DisbursedAt, d.Reference, d.Notes))
                    .ToList()))
            .ToListAsync(cancellationToken);

        return new PagedResult<GrantDto>(items, total, request.Page, request.PageSize);
    }
}
