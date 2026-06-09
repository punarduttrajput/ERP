using ERP.MobileApi.Application.Commands;
using ERP.MobileApi.Application.Queries;
using ERP.MobileApi.Application.Services;
using ERP.MobileApi.Domain;
using ERP.MobileApi.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace ERP.UnitTests.MobileApi;

public class MobileApiTests
{
    private static IMobileDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<TestMobileDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new TestMobileDbContext(options);
    }

    [Fact]
    public async Task RegisterDevice_NewToken_InsertsRegistration()
    {
        var db = CreateInMemoryDb();
        var handler = new RegisterDeviceHandler(db);
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var result = await handler.Handle(
            new RegisterDeviceCommand(tenantId, userId, "token-abc", DevicePlatform.Android, "1.0"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var reg = await ((TestMobileDbContext)db).DeviceRegistrations.FirstAsync();
        reg.IsActive.Should().BeTrue();
        reg.DeviceToken.Should().Be("token-abc");
        reg.Platform.Should().Be(DevicePlatform.Android);
    }

    [Fact]
    public async Task RegisterDevice_ExistingToken_UpdatesLastSeen()
    {
        var db = CreateInMemoryDb();
        var handler = new RegisterDeviceHandler(db);
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var token = "token-xyz";

        // First registration
        var firstCall = DateTime.UtcNow.AddMinutes(-10);
        var existing = new DeviceRegistration
        {
            TenantId = tenantId,
            UserId = userId,
            DeviceToken = token,
            Platform = DevicePlatform.iOS,
            IsActive = true,
            RegisteredAt = firstCall,
            LastSeenAt = firstCall
        };
        await db.DeviceRegistrations.AddAsync(existing);
        await db.SaveChangesAsync(CancellationToken.None);

        var beforeSecond = DateTime.UtcNow;
        await handler.Handle(
            new RegisterDeviceCommand(tenantId, userId, token, DevicePlatform.iOS, "2.0"),
            CancellationToken.None);

        var reg = await ((TestMobileDbContext)db).DeviceRegistrations.FirstAsync();
        reg.IsActive.Should().BeTrue();
        reg.LastSeenAt.Should().BeOnOrAfter(beforeSecond);
        reg.AppVersion.Should().Be("2.0");
    }

    [Fact]
    public async Task UnregisterDevice_SetsInactive()
    {
        var db = CreateInMemoryDb();
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var token = "token-to-remove";

        var reg = new DeviceRegistration
        {
            TenantId = tenantId,
            UserId = userId,
            DeviceToken = token,
            Platform = DevicePlatform.Web,
            IsActive = true,
            RegisteredAt = DateTime.UtcNow,
            LastSeenAt = DateTime.UtcNow
        };
        await db.DeviceRegistrations.AddAsync(reg);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new UnregisterDeviceHandler(db);
        var result = await handler.Handle(
            new UnregisterDeviceCommand(tenantId, userId, token),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var updated = await ((TestMobileDbContext)db).DeviceRegistrations.FirstAsync();
        updated.IsActive.Should().BeFalse();
    }

    [Fact]
    public void AttendanceSummary_Calculation()
    {
        int totalClasses = 10;
        int present = 8;
        var percent = totalClasses > 0 ? Math.Round((decimal)present / totalClasses * 100, 2) : 0m;

        var dto = new AttendanceSummaryMobileDto(totalClasses, present, percent);

        dto.Percent.Should().Be(80.0m);
        dto.TotalClasses.Should().Be(10);
        dto.Present.Should().Be(8);
    }

    [Fact]
    public void FeeStatus_NoDues_IsFullyPaidTrue()
    {
        var dto = new FeeStatusDto(0m, true);

        dto.DueAmount.Should().Be(0m);
        dto.IsFullyPaid.Should().BeTrue();
    }

    [Fact]
    public async Task SendNotification_RecordsInDb()
    {
        var db = CreateInMemoryDb();
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var token = "fcm-token-001";

        // Register a device so the handler finds active recipients
        await db.DeviceRegistrations.AddAsync(new DeviceRegistration
        {
            TenantId = tenantId,
            UserId = userId,
            DeviceToken = token,
            Platform = DevicePlatform.Android,
            IsActive = true,
            RegisteredAt = DateTime.UtcNow,
            LastSeenAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync(CancellationToken.None);

        var mockPushService = new Mock<IPushNotificationService>();
        mockPushService
            .Setup(s => s.SendToUsersAsync(
                It.IsAny<Guid>(),
                It.IsAny<IReadOnlyList<Guid>>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IDictionary<string, string>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = new SendPushNotificationHandler(db, mockPushService.Object);
        var result = await handler.Handle(
            new SendPushNotificationCommand(
                tenantId,
                new[] { userId },
                "Test Title",
                "Test Body",
                null,
                "FeeAlert"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var notification = await ((TestMobileDbContext)db).PushNotifications.FirstAsync();
        notification.Status.Should().Be(NotificationStatus.Sent);
        notification.Title.Should().Be("Test Title");
        notification.NotificationType.Should().Be("FeeAlert");
    }

    // In-memory EF context used only in tests — avoids pulling in the full AppDbContext
    private sealed class TestMobileDbContext : DbContext, IMobileDbContext
    {
        public TestMobileDbContext(DbContextOptions<TestMobileDbContext> options) : base(options) { }

        public DbSet<DeviceRegistration> DeviceRegistrations => Set<DeviceRegistration>();
        public DbSet<PushNotification> PushNotifications => Set<PushNotification>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // Unique index cannot be enforced by InMemory provider, but the schema still describes it
            modelBuilder.Entity<DeviceRegistration>().HasKey(x => x.Id);
            modelBuilder.Entity<PushNotification>().HasKey(x => x.Id);
        }
    }
}
