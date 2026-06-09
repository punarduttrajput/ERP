using ERP.Shared.Application.Common;
using MediatR;

namespace ERP.RBAC.Application.Commands;

public record AssignUserRoleCommand(Guid UserId, Guid RoleId) : IRequest<Result>;
