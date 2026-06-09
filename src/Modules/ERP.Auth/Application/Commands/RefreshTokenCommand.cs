using ERP.Auth.API.Dtos;
using ERP.Shared.Application.Common;
using MediatR;

namespace ERP.Auth.Application.Commands;

public record RefreshTokenCommand(string Token, string? IpAddress) : IRequest<Result<LoginResponse>>;
