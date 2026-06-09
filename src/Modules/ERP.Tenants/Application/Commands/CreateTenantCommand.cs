using ERP.Shared.Application.Common;
using MediatR;

namespace ERP.Tenants.Application.Commands;

public record CreateTenantCommand(
    string Name,
    string Slug,
    string? ContactEmail,
    string? ContactPhone,
    string? Address,
    string? Plan
) : IRequest<Result<Guid>>;
