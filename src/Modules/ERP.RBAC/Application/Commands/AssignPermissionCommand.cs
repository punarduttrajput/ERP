using ERP.Shared.Application.Common;
using MediatR;

namespace ERP.RBAC.Application.Commands;

public record AssignPermissionCommand(Guid RoleId, Guid PermissionId) : IRequest<Result>;
