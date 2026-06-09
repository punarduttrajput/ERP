using ERP.Shared.Application.Common;
using MediatR;

namespace ERP.Auth.Application.Commands;

public record EnableMfaCommand(Guid UserId) : IRequest<Result<EnableMfaResponse>>;

public record EnableMfaResponse(string Secret, string QrCodeUri);
