using ERP.Research.Domain;
using ERP.Research.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Research.Application.Queries;

public record ResearchProjectDto(
    Guid Id,
    string Title,
    Guid PrincipalInvestigatorId,
    string PrincipalInvestigatorName,
    string FundingAgency,
    string? FundingScheme,
    decimal SanctionedAmount,
    DateOnly StartDate,
    DateOnly? EndDate,
    ProjectStatus Status,
    string? SanctionNumber,
    string? Domain,
    IReadOnlyList<ProjectMemberDto> Members);

public record ProjectMemberDto(
    Guid Id,
    Guid UserId,
    string MemberName,
    MemberRole Role,
    DateOnly JoinedAt,
    DateOnly? LeftAt);

public record GetResearchProjectsQuery(
    Guid TenantId,
    ProjectStatus? Status,
    Guid? PiId,
    string? Domain,
    int Page,
    int PageSize) : IRequest<PagedResult<ResearchProjectDto>>;

public class GetResearchProjectsHandler : IRequestHandler<GetResearchProjectsQuery, PagedResult<ResearchProjectDto>>
{
    private readonly IResearchDbContext _db;

    public GetResearchProjectsHandler(IResearchDbContext db) => _db = db;

    public async Task<PagedResult<ResearchProjectDto>> Handle(GetResearchProjectsQuery request, CancellationToken cancellationToken)
    {
        var query = _db.ResearchProjects
            .IgnoreQueryFilters()
            .Include(x => x.Members)
            .Where(x => x.TenantId == request.TenantId && !x.IsDeleted);

        if (request.Status.HasValue)
            query = query.Where(x => x.Status == request.Status.Value);

        if (request.PiId.HasValue)
            query = query.Where(x => x.PrincipalInvestigatorId == request.PiId.Value);

        if (!string.IsNullOrWhiteSpace(request.Domain))
            query = query.Where(x => x.Domain == request.Domain);

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new ResearchProjectDto(
                x.Id, x.Title, x.PrincipalInvestigatorId, x.PrincipalInvestigatorName,
                x.FundingAgency, x.FundingScheme, x.SanctionedAmount, x.StartDate, x.EndDate,
                x.Status, x.SanctionNumber, x.Domain,
                x.Members.Where(m => !m.IsDeleted).Select(m => new ProjectMemberDto(
                    m.Id, m.UserId, m.MemberName, m.Role, m.JoinedAt, m.LeftAt))
                    .ToList()))
            .ToListAsync(cancellationToken);

        return new PagedResult<ResearchProjectDto>(items, total, request.Page, request.PageSize);
    }
}
