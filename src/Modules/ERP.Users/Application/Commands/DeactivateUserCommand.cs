using ERP.Shared.Application.Common;
using MediatR;

namespace ERP.Users.Application.Commands;

public record DeactivateUserCommand(Guid UserId) : IRequest<Result>;
