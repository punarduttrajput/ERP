using ERP.Shared.Application.Common;
using MediatR;

namespace ERP.Auth.Application.Commands;

public record LogoutCommand(string RefreshToken) : IRequest<Result>;
