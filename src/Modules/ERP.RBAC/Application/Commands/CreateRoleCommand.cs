using ERP.Shared.Application.Common;
using MediatR;

namespace ERP.RBAC.Application.Commands;

public record CreateRoleCommand(string Name, string? Description) : IRequest<Result<Guid>>;
