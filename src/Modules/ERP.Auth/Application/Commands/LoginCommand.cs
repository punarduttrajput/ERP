using ERP.Auth.API.Dtos;
using ERP.Shared.Application.Common;
using MediatR;

namespace ERP.Auth.Application.Commands;

public record LoginCommand(
    string Email,
    string Password,
    string? IpAddress
) : IRequest<Result<LoginResponse>>;
