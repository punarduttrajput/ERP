using ERP.Shared.Application.Common;
using ERP.Tenants.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERP.Tenants.Application.Commands;

public sealed class SuspendTenantHandler : IRequestHandler<SuspendTenantCommand, Result>
{
    private readonly ITenantsDbContext _db;
    private readonly ILogger<SuspendTenantHandler> _logger;

    public SuspendTenantHandler(ITenantsDbContext db, ILogger<SuspendTenantHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Result> Handle(SuspendTenantCommand request, CancellationToken cancellationToken)
    {
        var tenant = await _db.Tenants
            .FirstOrDefaultAsync(t => t.Id == request.TenantId, cancellationToken);

        if (tenant is null)
            return Result.Failure($"Tenant '{request.TenantId}' not found.");

        if (tenant.Status == TenantStatus.Suspended)
            return Result.Failure("Tenant is already suspended.");

        tenant.Status = TenantStatus.Suspended;
        tenant.SuspendedAt = DateTime.UtcNow;
        tenant.SuspensionReason = request.Reason;
        tenant.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogWarning("Tenant suspended: {TenantId}, Reason: {Reason}", tenant.Id, request.Reason);

        return Result.Success();
    }
}
