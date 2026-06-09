using ERP.Shared.Application.Common;
using MediatR;

namespace ERP.Users.Application.Commands;

public record UpdateUserCommand(
    Guid UserId,
    string? FirstName,
    string? LastName,
    string? PhoneNumber,
    string? Department,
    string? JobTitle,
    string? AvatarUrl
) : IRequest<Result>;
