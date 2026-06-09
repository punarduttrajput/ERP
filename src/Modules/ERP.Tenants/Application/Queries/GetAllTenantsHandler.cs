using ERP.Shared.Application.Common;
using ERP.Tenants.API.Dtos;
using ERP.Tenants.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Tenants.Application.Queries;

public sealed class GetAllTenantsHandler : IRequestHandler<GetAllTenantsQuery, Result<PagedResult<TenantResponseDto>>>
{
    private readonly ITenantsReadDbContext _db;

    public GetAllTenantsHandler(ITenantsReadDbContext db)
    {
        _db = db;
    }

    public async Task<Result<PagedResult<TenantResponseDto>>> Handle(GetAllTenantsQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Tenants.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.ToLower();
            query = query.Where(t => t.Name.ToLower().Contains(search) || t.Slug.ToLower().Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(request.Status) && Enum.TryParse<TenantStatus>(request.Status, true, out var status))
        {
            query = query.Where(t => t.Status == status);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 20 : request.PageSize > 100 ? 100 : request.PageSize;

        var tenants = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new TenantResponseDto(
                t.Id, t.Name, t.Slug, t.LogoUrl, t.PrimaryColor,
                t.SecondaryColor, t.CustomDomain, t.Status.ToString(),
                t.ContactEmail, t.Plan, t.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result<PagedResult<TenantResponseDto>>.Success(
            new PagedResult<TenantResponseDto>(tenants, totalCount, page, pageSize));
    }
}
