using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERP.Tenants.Application.Commands;

public sealed class UpdateTenantBrandingHandler : IRequestHandler<UpdateTenantBrandingCommand, Result>
{
    private readonly ITenantsDbContext _db;
    private readonly ILogger<UpdateTenantBrandingHandler> _logger;

    public UpdateTenantBrandingHandler(ITenantsDbContext db, ILogger<UpdateTenantBrandingHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Result> Handle(UpdateTenantBrandingCommand request, CancellationToken cancellationToken)
    {
        var tenant = await _db.Tenants
            .Include(t => t.Settings)
            .FirstOrDefaultAsync(t => t.Id == request.TenantId, cancellationToken);

        if (tenant is null)
            return Result.Failure($"Tenant '{request.TenantId}' not found.");

        if (!string.IsNullOrWhiteSpace(request.CustomDomain))
        {
            var domainTaken = await _db.Tenants
                .AnyAsync(t => t.CustomDomain == request.CustomDomain && t.Id != request.TenantId, cancellationToken);
            if (domainTaken)
                return Result.Failure($"Custom domain '{request.CustomDomain}' is already in use.");
        }

        tenant.LogoUrl = request.LogoUrl ?? tenant.LogoUrl;
        tenant.PrimaryColor = request.PrimaryColor ?? tenant.PrimaryColor;
        tenant.SecondaryColor = request.SecondaryColor ?? tenant.SecondaryColor;
        tenant.CustomDomain = request.CustomDomain ?? tenant.CustomDomain;
        tenant.UpdatedAt = DateTime.UtcNow;

        if (tenant.Settings != null && request.CustomCss != null)
        {
            tenant.Settings.CustomCss = request.CustomCss;
            tenant.Settings.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Tenant branding updated: {TenantId}", tenant.Id);

        return Result.Success();
    }
}
