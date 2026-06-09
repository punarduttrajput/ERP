using ERP.Shared.Application.Common;
using MediatR;

namespace ERP.Auth.Application.Commands;

public record ConfirmMfaCommand(Guid UserId, string TotpCode) : IRequest<Result<ConfirmMfaResponse>>;

public record ConfirmMfaResponse(IReadOnlyList<string> RecoveryCodes);
