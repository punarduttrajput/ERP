using ERP.Shared.Application.Common;
using ERP.Tenants.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERP.Tenants.Application.Commands;

public sealed class CreateTenantHandler : IRequestHandler<CreateTenantCommand, Result<Guid>>
{
    private readonly ITenantsDbContext _db;
    private readonly ILogger<CreateTenantHandler> _logger;

    public CreateTenantHandler(ITenantsDbContext db, ILogger<CreateTenantHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(CreateTenantCommand request, CancellationToken cancellationToken)
    {
        var slugExists = await _db.Tenants
            .AnyAsync(t => t.Slug == request.Slug.ToLowerInvariant(), cancellationToken);

        if (slugExists)
            return Result<Guid>.Failure($"Tenant slug '{request.Slug}' is already taken.");

        var tenant = new Tenant
        {
            Name = request.Name,
            Slug = request.Slug.ToLowerInvariant(),
            ContactEmail = request.ContactEmail,
            ContactPhone = request.ContactPhone,
            Address = request.Address,
            Plan = request.Plan ?? "standard",
            Status = TenantStatus.Active
        };

        var settings = new TenantSettings
        {
            TenantId = tenant.Id,
            Tenant = tenant
        };

        tenant.Settings = settings;

        await _db.Tenants.AddAsync(tenant, cancellationToken);
        await _db.TenantSettings.AddAsync(settings, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Tenant created: {TenantId} ({Slug})", tenant.Id, tenant.Slug);

        return Result<Guid>.Success(tenant.Id);
    }
}
