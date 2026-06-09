using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Common;
using ERP.Tenants.API.Dtos;
using ERP.Tenants.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Tenants.Application.Queries;

public sealed class GetTenantHandler : IRequestHandler<GetTenantQuery, Result<TenantResponseDto>>
{
    private readonly ITenantsReadDbContext _db;

    public GetTenantHandler(ITenantsReadDbContext db)
    {
        _db = db;
    }

    public async Task<Result<TenantResponseDto>> Handle(GetTenantQuery request, CancellationToken cancellationToken)
    {
        var tenant = await _db.Tenants
            .AsNoTracking()
            .Include(t => t.Settings)
            .FirstOrDefaultAsync(t => t.Id == request.TenantId, cancellationToken);

        if (tenant is null)
            return Result<TenantResponseDto>.Failure($"Tenant '{request.TenantId}' not found.");

        return Result<TenantResponseDto>.Success(MapToDto(tenant));
    }

    private static TenantResponseDto MapToDto(Tenant tenant) => new(
        tenant.Id,
        tenant.Name,
        tenant.Slug,
        tenant.LogoUrl,
        tenant.PrimaryColor,
        tenant.SecondaryColor,
        tenant.CustomDomain,
        tenant.Status.ToString(),
        tenant.ContactEmail,
        tenant.Plan,
        tenant.CreatedAt
    );
}
