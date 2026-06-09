using ERP.Shared.Application.Common;
using MediatR;

namespace ERP.Tenants.Application.Commands;

public record SuspendTenantCommand(Guid TenantId, string Reason) : IRequest<Result>;
