using ERP.Shared.Application.Common;
using MediatR;

namespace ERP.Tenants.Application.Commands;

public record UpdateTenantBrandingCommand(
    Guid TenantId,
    string? LogoUrl,
    string? PrimaryColor,
    string? SecondaryColor,
    string? CustomDomain,
    string? CustomCss
) : IRequest<Result>;
