using ERP.Research.Domain;
using ERP.Research.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Research.Application.Queries;

public record ResearchDashboardDto(
    int ActiveProjects,
    int TotalPublications,
    int ScopusIndexedPublications,
    int GrantedPatents,
    decimal TotalFundingReceived,
    decimal TotalFundingUtilized,
    int ActiveGrants);

public record GetResearchDashboardQuery(Guid TenantId) : IRequest<Result<ResearchDashboardDto>>;

public class GetResearchDashboardHandler : IRequestHandler<GetResearchDashboardQuery, Result<ResearchDashboardDto>>
{
    private readonly IResearchDbContext _db;

    public GetResearchDashboardHandler(IResearchDbContext db) => _db = db;

    public async Task<Result<ResearchDashboardDto>> Handle(GetResearchDashboardQuery request, CancellationToken cancellationToken)
    {
        var tenantId = request.TenantId;

        var activeProjects = await _db.ResearchProjects
            .IgnoreQueryFilters()
            .CountAsync(x => x.TenantId == tenantId && !x.IsDeleted && x.Status == ProjectStatus.Active, cancellationToken);

        var totalPublications = await _db.Publications
            .IgnoreQueryFilters()
            .CountAsync(x => x.TenantId == tenantId && !x.IsDeleted, cancellationToken);

        var scopusIndexed = await _db.Publications
            .IgnoreQueryFilters()
            .CountAsync(x => x.TenantId == tenantId && !x.IsDeleted && x.Index == PublicationIndex.Scopus, cancellationToken);

        var grantedPatents = await _db.Patents
            .IgnoreQueryFilters()
            .CountAsync(x => x.TenantId == tenantId && !x.IsDeleted && x.Status == PatentStatus.Granted, cancellationToken);

        var totalFundingReceived = await _db.Grants
            .IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && !x.IsDeleted)
            .SumAsync(x => x.DisbursedAmount, cancellationToken);

        var totalFundingUtilized = await _db.Grants
            .IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && !x.IsDeleted)
            .SumAsync(x => x.UtilizedAmount, cancellationToken);

        var activeGrants = await _db.Grants
            .IgnoreQueryFilters()
            .CountAsync(x => x.TenantId == tenantId && !x.IsDeleted && x.Status == GrantStatus.Active, cancellationToken);

        return Result<ResearchDashboardDto>.Success(new ResearchDashboardDto(
            activeProjects,
            totalPublications,
            scopusIndexed,
            grantedPatents,
            totalFundingReceived,
            totalFundingUtilized,
            activeGrants));
    }
}
