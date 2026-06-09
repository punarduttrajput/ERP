using ERP.Shared.Application.Common;
using MediatR;

namespace ERP.Auth.Application.Commands;

public record SendOtpCommand(string MobileNumber) : IRequest<Result>;
