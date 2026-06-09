using ERP.Shared.Application.Abstractions;
using ERP.Transport.Application.Commands;
using ERP.Transport.Application.Jobs;
using ERP.Transport.Domain;
using ERP.Transport.Infrastructure;
using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;

namespace ERP.UnitTests.Transport;

// Minimal in-memory context implementing ITransportDbContext.
// Cannot use AppDbContext here — test project does not reference ERP.Host.
internal class TestTransportDbContext : DbContext, ITransportDbContext
{
    public TestTransportDbContext(DbContextOptions options) : base(options) { }

    public DbSet<Route> Routes => Set<Route>();
    public DbSet<RouteStop> RouteStops => Set<RouteStop>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<Driver> Drivers => Set<Driver>();
    public DbSet<RouteAssignment> RouteAssignments => Set<RouteAssignment>();
    public DbSet<GpsLocation> GpsLocations => Set<GpsLocation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Route>().HasMany(r => r.Stops).WithOne(s => s.Route).HasForeignKey(s => s.RouteId);
    }
}

public class TransportTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    private static TestTransportDbContext BuildDb() =>
        new(new DbContextOptionsBuilder()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static ICurrentTenant MockTenant()
    {
        var mock = new Mock<ICurrentTenant>();
        mock.Setup(t => t.TenantId).Returns(TenantId);
        return mock.Object;
    }

    private static Route SeedRoute(TestTransportDbContext db)
    {
        var route = new Route
        {
            TenantId = TenantId,
            Name = "Route 1",
            DepartureTime = new TimeOnly(8, 0),
            ReturnTime = new TimeOnly(17, 0)
        };
        db.Routes.Add(route);
        db.SaveChanges();
        return route;
    }

    private static (Route route, RouteStop stop) SeedRouteWithStop(TestTransportDbContext db, int sequence = 1)
    {
        var route = SeedRoute(db);
        var stop = new RouteStop
        {
            TenantId = TenantId,
            RouteId = route.Id,
            Name = "City Bus Stand",
            Sequence = sequence
        };
        db.RouteStops.Add(stop);
        route.TotalStops = 1;
        db.SaveChanges();
        return (route, stop);
    }

    // --- Story 3.6.1: Route and Stop Configuration ---

    [Fact]
    public async Task AddStop_DuplicateSequence_ReturnsFailure()
    {
        using var db = BuildDb();
        var (route, _) = SeedRouteWithStop(db, sequence: 1);
        var handler = new AddRouteStopCommandHandler(db, MockTenant());

        var result = await handler.Handle(
            new AddRouteStopCommand(route.Id, "Railway Station", 1, null, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Sequence 1");
    }

    [Fact]
    public async Task AddStop_ValidSequence_IncrementsRouteStops()
    {
        using var db = BuildDb();
        var route = SeedRoute(db);
        var handler = new AddRouteStopCommandHandler(db, MockTenant());

        var result = await handler.Handle(
            new AddRouteStopCommand(route.Id, "City Bus Stand", 1, new TimeOnly(7, 30), 5.5m),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var updatedRoute = await db.Routes.FindAsync(route.Id);
        updatedRoute!.TotalStops.Should().Be(1);

        var stop = await db.RouteStops.FindAsync(result.Value);
        stop.Should().NotBeNull();
        stop!.Name.Should().Be("City Bus Stand");
    }

    // --- Story 3.6.3: Student/Staff Assignment ---

    [Fact]
    public async Task AssignToRoute_DuplicateMember_ReturnsFailure()
    {
        using var db = BuildDb();
        var (route, stop) = SeedRouteWithStop(db);
        var memberId = Guid.NewGuid();

        db.RouteAssignments.Add(new RouteAssignment
        {
            TenantId = TenantId,
            RouteId = route.Id,
            StopId = stop.Id,
            MemberId = memberId,
            MemberType = "Student",
            MemberName = "John Doe",
            AcademicYear = 2024
        });
        await db.SaveChangesAsync();

        var handler = new AssignToRouteCommandHandler(db, MockTenant());

        var result = await handler.Handle(
            new AssignToRouteCommand(route.Id, stop.Id, memberId, "Student", "John Doe", 2024),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("already assigned");
    }

    [Fact]
    public async Task AssignToRoute_Valid_IncrementsTotalPassengers()
    {
        using var db = BuildDb();
        var (route, stop) = SeedRouteWithStop(db);
        var handler = new AssignToRouteCommandHandler(db, MockTenant());

        var result = await handler.Handle(
            new AssignToRouteCommand(route.Id, stop.Id, Guid.NewGuid(), "Student", "Jane Smith", 2024),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var updatedRoute = await db.Routes.FindAsync(route.Id);
        updatedRoute!.TotalPassengers.Should().Be(1);
    }

    // --- Story 3.6.2: Compliance Alerts ---

    [Fact]
    public async Task ComplianceAlert_ExpiringIn15Days_SendsSms()
    {
        using var db = BuildDb();
        var expiryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(15));
        db.Vehicles.Add(new Vehicle
        {
            TenantId = TenantId,
            RegistrationNumber = "TN01AB1234",
            Make = "Tata",
            Model = "Starbus",
            Capacity = 40,
            FitnessExpiryDate = expiryDate,
            InsuranceExpiryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(100)),
            PollutionExpiryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(100)),
            IsActive = true
        });
        await db.SaveChangesAsync();

        var smsMock = new Mock<ISmsService>();
        var redisMock = new Mock<IConnectionMultiplexer>();
        var dbMock = new Mock<IDatabase>();

        // Redis cache miss — key doesn't exist yet, so alert fires
        dbMock.Setup(d => d.KeyExistsAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(false);
        dbMock.Setup(d => d.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(),
            It.IsAny<TimeSpan?>(), It.IsAny<bool>(), It.IsAny<When>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);
        redisMock.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(dbMock.Object);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Transport:ManagerMobile"] = "9999999999" })
            .Build();

        var job = new ComplianceAlertJob(db, smsMock.Object, redisMock.Object, config,
            Mock.Of<ILogger<ComplianceAlertJob>>());

        await job.RunAsync(CancellationToken.None);

        smsMock.Verify(s => s.SendAsync(
            "9999999999",
            It.Is<string>(m => m.Contains("TN01AB1234") && m.Contains("fitness")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ComplianceAlert_ExpiringIn45Days_NoSms()
    {
        using var db = BuildDb();
        var expiryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(45));
        db.Vehicles.Add(new Vehicle
        {
            TenantId = TenantId,
            RegistrationNumber = "TN02CD5678",
            Make = "Ashok Leyland",
            Model = "Viking",
            Capacity = 50,
            FitnessExpiryDate = expiryDate,
            InsuranceExpiryDate = expiryDate,
            PollutionExpiryDate = expiryDate,
            IsActive = true
        });
        await db.SaveChangesAsync();

        var smsMock = new Mock<ISmsService>();
        var redisMock = new Mock<IConnectionMultiplexer>();
        redisMock.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(Mock.Of<IDatabase>());

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Transport:ManagerMobile"] = "9999999999" })
            .Build();

        var job = new ComplianceAlertJob(db, smsMock.Object, redisMock.Object, config,
            Mock.Of<ILogger<ComplianceAlertJob>>());

        await job.RunAsync(CancellationToken.None);

        smsMock.Verify(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // --- Story 3.6.4: GPS Integration ---

    [Fact]
    public async Task GpsWebhook_ValidPayload_InsertsLocation()
    {
        using var db = BuildDb();
        var vehicleId = Guid.NewGuid();
        db.Vehicles.Add(new Vehicle
        {
            Id = vehicleId,
            TenantId = TenantId,
            RegistrationNumber = "TN01AB1234",
            Make = "Tata",
            Model = "Starbus",
            Capacity = 40,
            FitnessExpiryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(200)),
            InsuranceExpiryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(200)),
            PollutionExpiryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(200))
        });
        await db.SaveChangesAsync();

        var provider = new NullGpsProvider();
        var payload = """{"reg":"TN01AB1234","lat":13.0827,"lng":80.2707,"speed":45.0}""";
        var update = provider.ParseWebhook(payload);
        update.Should().NotBeNull();
        update!.VehicleRegistration.Should().Be("TN01AB1234");

        var handler = new UpdateGpsLocationCommandHandler(db, MockTenant());
        var result = await handler.Handle(
            new UpdateGpsLocationCommand(vehicleId, update.Latitude, update.Longitude,
                update.Speed, update.Heading, update.RecordedAt, update.ProviderReference),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var location = await db.GpsLocations.FirstOrDefaultAsync();
        location.Should().NotBeNull();
        location!.VehicleId.Should().Be(vehicleId);
        location.Latitude.Should().Be(13.0827m);
        location.Longitude.Should().Be(80.2707m);
        location.Speed.Should().Be(45.0m);
    }
}
