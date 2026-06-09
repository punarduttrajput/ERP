using ERP.Shared.Application.Common;
using MediatR;

namespace ERP.RBAC.Application.Queries;

public record RoleDto(Guid Id, string Name, string? Description, bool IsSystemRole);

public record GetRolesQuery(int Page = 1, int PageSize = 50) : IRequest<Result<PagedResult<RoleDto>>>;
