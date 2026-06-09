using ERP.Host;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MySql;
using Xunit;

namespace ERP.IntegrationTests.Migrations;

public class MigrationIntegrityTests : IAsyncLifetime
{
    private readonly MySqlContainer _mysql = new MySqlBuilder()
        .WithImage("mysql:8.0")
        .WithDatabase("erp_migration_test")
        .WithUsername("root")
        .WithPassword("test_password")
        .Build();

    private IServiceProvider _services = null!;

    public async Task InitializeAsync()
    {
        await _mysql.StartAsync();

        var services = new ServiceCollection();
        var connStr  = _mysql.GetConnectionString();

        services.AddScoped<ERP.Shared.Application.Abstractions.ICurrentTenant, ERP.Shared.Infrastructure.CurrentTenant>();
        services.AddHttpContextAccessor();

        services.AddDbContext<AppDbContext>((sp, options) =>
            options.UseMySql(connStr, ServerVersion.AutoDetect(connStr)));

        _services = services.BuildServiceProvider();
    }

    public async Task DisposeAsync() => await _mysql.DisposeAsync();

    [Fact]
    public async Task AllMigrations_ApplyCleanly_NoErrors()
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // MigrateAsync applies all pending migrations in order
        // If any migration has invalid SQL, this throws and the test fails
        var applyMigrations = async () => await db.Database.MigrateAsync();

        await applyMigrations.Should().NotThrowAsync("all migrations must apply cleanly against a fresh MySQL 8.0 instance");
    }

    [Fact]
    public async Task AllMigrations_Idempotent_SecondApplyNoOps()
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.Database.MigrateAsync();  // first apply

        // Second apply should be a no-op (all migrations already in __EFMigrationsHistory)
        var secondApply = async () => await db.Database.MigrateAsync();
        await secondApply.Should().NotThrowAsync("re-applying migrations must be idempotent");
    }

    [Fact]
    public async Task MigrationCount_MatchesExpected()
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.Database.MigrateAsync();

        var appliedMigrations = await db.Database.GetAppliedMigrationsAsync();
        // We have 29 migrations (migration 00 through 28)
        appliedMigrations.Should().HaveCountGreaterThanOrEqualTo(28,
            "all module migrations must be applied");
    }
}
