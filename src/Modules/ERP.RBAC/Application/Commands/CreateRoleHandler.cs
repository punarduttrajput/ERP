using ERP.RBAC.Domain;
using ERP.RBAC.Infrastructure;
using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.RBAC.Application.Commands;

public sealed class CreateRoleHandler : IRequestHandler<CreateRoleCommand, Result<Guid>>
{
    private readonly IRbacDbContext _db;
    private readonly ICurrentTenant _currentTenant;

    public CreateRoleHandler(IRbacDbContext db, ICurrentTenant currentTenant)
    {
        _db = db;
        _currentTenant = currentTenant;
    }

    public async Task<Result<Guid>> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        if (!_currentTenant.TenantId.HasValue)
            return Result<Guid>.Failure("Tenant context is required.");

        var tenantId = _currentTenant.TenantId.Value;

        var exists = await _db.Roles
            .AnyAsync(r => r.TenantId == tenantId && r.Name == request.Name, cancellationToken);

        if (exists)
            return Result<Guid>.Failure($"Role '{request.Name}' already exists.");

        var role = new Role
        {
            TenantId = tenantId,
            Name = request.Name,
            Description = request.Description
        };

        await _db.Roles.AddAsync(role, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(role.Id);
    }
}
