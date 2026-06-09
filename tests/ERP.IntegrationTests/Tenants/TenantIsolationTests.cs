using ERP.Host;
using ERP.Shared.Infrastructure;
using ERP.Tenants.Domain;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MySql;
using Testcontainers.Redis;
using Xunit;

namespace ERP.IntegrationTests.Tenants;

public class TenantIsolationTests : IAsyncLifetime
{
    private readonly MySqlContainer _mySqlContainer = new MySqlBuilder()
        .WithImage("mysql:8.0")
        .WithDatabase("erp_test")
        .WithUsername("root")
        .WithPassword("test_password")
        .Build();

    private readonly RedisContainer _redisContainer = new RedisBuilder()
        .WithImage("redis:7-alpine")
        .Build();

    private IServiceProvider _serviceProvider = null!;

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_mySqlContainer.StartAsync(), _redisContainer.StartAsync());

        var services = new ServiceCollection();

        var connStr = _mySqlContainer.GetConnectionString();

        services.AddScoped<ERP.Shared.Application.Abstractions.ICurrentTenant, CurrentTenant>();
        services.AddScoped<ERP.Shared.Application.Abstractions.ICurrentUser>(_ =>
            new MockCurrentUser());

        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            var currentTenant = sp.GetRequiredService<ERP.Shared.Application.Abstractions.ICurrentTenant>();
            options.UseMySql(connStr, ServerVersion.AutoDetect(connStr));
        });

        _serviceProvider = services.BuildServiceProvider();

        // Run migrations against the real schema (not a model-derived snapshot)
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _mySqlContainer.DisposeAsync();
        await _redisContainer.DisposeAsync();
    }

    [Fact]
    public async Task Tenant_Data_ShouldBeIsolated_BetweenTenants()
    {
        var tenantAId = Guid.NewGuid();
        var tenantBId = Guid.NewGuid();

        // Create tenants (no tenant filter on Tenant table itself)
        using (var scope = _serviceProvider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Tenants.Add(new Tenant { Id = tenantAId, Name = "University A", Slug = "uni-a", Status = TenantStatus.Active });
            db.Tenants.Add(new Tenant { Id = tenantBId, Name = "University B", Slug = "uni-b", Status = TenantStatus.Active });
            await db.SaveChangesAsync();
        }

        // Create roles for Tenant A
        using (var scope = _serviceProvider.CreateScope())
        {
            var currentTenant = scope.ServiceProvider.GetRequiredService<ERP.Shared.Application.Abstractions.ICurrentTenant>();
            currentTenant.SetTenant(tenantAId, "uni-a");
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            db.Roles.Add(new ERP.RBAC.Domain.Role { TenantId = tenantAId, Name = "Admin A" });
            await db.SaveChangesAsync();
        }

        // Create roles for Tenant B
        using (var scope = _serviceProvider.CreateScope())
        {
            var currentTenant = scope.ServiceProvider.GetRequiredService<ERP.Shared.Application.Abstractions.ICurrentTenant>();
            currentTenant.SetTenant(tenantBId, "uni-b");
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            db.Roles.Add(new ERP.RBAC.Domain.Role { TenantId = tenantBId, Name = "Admin B" });
            await db.SaveChangesAsync();
        }

        // Verify Tenant A only sees its own roles
        using (var scope = _serviceProvider.CreateScope())
        {
            var currentTenant = scope.ServiceProvider.GetRequiredService<ERP.Shared.Application.Abstractions.ICurrentTenant>();
            currentTenant.SetTenant(tenantAId, "uni-a");
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var rolesForA = await db.Roles.ToListAsync();
            rolesForA.Should().HaveCount(1);
            rolesForA.First().Name.Should().Be("Admin A");
            rolesForA.All(r => r.TenantId == tenantAId).Should().BeTrue();
        }

        // Verify Tenant B only sees its own roles
        using (var scope = _serviceProvider.CreateScope())
        {
            var currentTenant = scope.ServiceProvider.GetRequiredService<ERP.Shared.Application.Abstractions.ICurrentTenant>();
            currentTenant.SetTenant(tenantBId, "uni-b");
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var rolesForB = await db.Roles.ToListAsync();
            rolesForB.Should().HaveCount(1);
            rolesForB.First().Name.Should().Be("Admin B");
            rolesForB.All(r => r.TenantId == tenantBId).Should().BeTrue();
        }
    }

    [Fact]
    public async Task SoftDeleted_Records_ShouldNotBeVisible()
    {
        var tenantId = Guid.NewGuid();

        // Create a tenant and role
        using (var scope = _serviceProvider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Tenants.Add(new Tenant { Id = tenantId, Name = "University C", Slug = "uni-c", Status = TenantStatus.Active });
            await db.SaveChangesAsync();
        }

        Guid roleId;
        using (var scope = _serviceProvider.CreateScope())
        {
            var currentTenant = scope.ServiceProvider.GetRequiredService<ERP.Shared.Application.Abstractions.ICurrentTenant>();
            currentTenant.SetTenant(tenantId, "uni-c");
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var role = new ERP.RBAC.Domain.Role { TenantId = tenantId, Name = "DeleteMe" };
            db.Roles.Add(role);
            await db.SaveChangesAsync();
            roleId = role.Id;
        }

        // Soft delete
        using (var scope = _serviceProvider.CreateScope())
        {
            var currentTenant = scope.ServiceProvider.GetRequiredService<ERP.Shared.Application.Abstractions.ICurrentTenant>();
            currentTenant.SetTenant(tenantId, "uni-c");
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var role = await db.Roles.FindAsync(roleId);
            role!.IsDeleted = true;
            await db.SaveChangesAsync();
        }

        // Should not be visible
        using (var scope = _serviceProvider.CreateScope())
        {
            var currentTenant = scope.ServiceProvider.GetRequiredService<ERP.Shared.Application.Abstractions.ICurrentTenant>();
            currentTenant.SetTenant(tenantId, "uni-c");
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var roles = await db.Roles.ToListAsync();
            roles.Any(r => r.Id == roleId).Should().BeFalse();
        }
    }
}

internal class MockCurrentUser : ERP.Shared.Application.Abstractions.ICurrentUser
{
    public static readonly Guid FixedUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public Guid? UserId => FixedUserId;
    public string? Email => "admin@test.com";
    public string? Name => "Test Admin";
    public Guid? TenantId => null;
    public IReadOnlyList<string> Roles => new List<string> { "Admin" };
    public bool IsAuthenticated => true;
    public bool HasPermission(string permission) => true;
}
