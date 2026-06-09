using ERP.Auth.API.Dtos;
using ERP.Shared.Application.Common;
using MediatR;

namespace ERP.Auth.Application.Commands;

public record VerifyMfaLoginCommand(
    string MfaChallengeToken,
    string Code,          // TOTP code OR recovery code
    string? IpAddress
) : IRequest<Result<LoginResponse>>;
