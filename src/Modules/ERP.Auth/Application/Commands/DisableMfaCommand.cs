using ERP.Shared.Application.Common;
using MediatR;

namespace ERP.Auth.Application.Commands;

public record DisableMfaCommand(Guid UserId, string TotpCode) : IRequest<Result>;
