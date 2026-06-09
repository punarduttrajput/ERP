using ERP.Shared.Application.Common;
using MediatR;

namespace ERP.Users.Application.Commands;

public record CreateUserCommand(
    string Email,
    string Password,
    string? FirstName,
    string? LastName,
    string? PhoneNumber,
    string? Department,
    string? JobTitle
) : IRequest<Result<Guid>>;
