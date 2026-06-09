using ERP.RBAC.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.RBAC.Application.Queries;

public sealed class GetRolesHandler : IRequestHandler<GetRolesQuery, Result<PagedResult<RoleDto>>>
{
    private readonly IRbacDbContext _db;

    public GetRolesHandler(IRbacDbContext db)
    {
        _db = db;
    }

    public async Task<Result<PagedResult<RoleDto>>> Handle(GetRolesQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Roles.AsNoTracking();
        var totalCount = await query.CountAsync(cancellationToken);
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 50 : request.PageSize > 200 ? 200 : request.PageSize;

        var roles = await query
            .OrderBy(r => r.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new RoleDto(r.Id, r.Name, r.Description, r.IsSystemRole))
            .ToListAsync(cancellationToken);

        return Result<PagedResult<RoleDto>>.Success(
            new PagedResult<RoleDto>(roles, totalCount, page, pageSize));
    }
}
