using ERP.Placement.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Placement.Application.Queries;

public record CompanyDto(
    Guid Id,
    string Name,
    string Industry,
    string? Website,
    string? ContactPersonName,
    string? ContactEmail,
    int TotalDrives,
    int TotalOffers,
    decimal HighestPackageLpa,
    decimal AveragePackageLpa,
    bool IsActive
);

public record GetCompaniesQuery(
    int Page,
    int PageSize,
    string? Industry,
    bool? IsActive
) : IRequest<PagedResult<CompanyDto>>;

public class GetCompaniesHandler : IRequestHandler<GetCompaniesQuery, PagedResult<CompanyDto>>
{
    private readonly IPlacementDbContext _db;

    public GetCompaniesHandler(IPlacementDbContext db) => _db = db;

    public async Task<PagedResult<CompanyDto>> Handle(GetCompaniesQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Companies.AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Industry))
            query = query.Where(x => x.Industry == request.Industry);

        if (request.IsActive.HasValue)
            query = query.Where(x => x.IsActive == request.IsActive.Value);

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(x => x.Name)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new CompanyDto(
                x.Id, x.Name, x.Industry, x.Website,
                x.ContactPersonName, x.ContactEmail,
                x.TotalDrives, x.TotalOffers,
                x.HighestPackageLpa, x.AveragePackageLpa, x.IsActive))
            .ToListAsync(cancellationToken);

        return new PagedResult<CompanyDto>(items, total, request.Page, request.PageSize);
    }
}
