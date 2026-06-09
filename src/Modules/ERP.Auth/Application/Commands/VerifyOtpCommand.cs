using ERP.Auth.API.Dtos;
using ERP.Shared.Application.Common;
using MediatR;

namespace ERP.Auth.Application.Commands;

public record VerifyOtpCommand(string MobileNumber, string Otp, string? IpAddress) : IRequest<Result<LoginResponse>>;
