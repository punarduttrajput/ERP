using ERP.Placement.Application.Commands;
using ERP.Placement.Domain;
using ERP.Placement.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ERP.UnitTests.Placement;

public class PlacementTests
{
    private static IPlacementDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<PlacementTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new PlacementTestDbContext(options);
    }

    private static (Company company, PlacementDrive drive) SeedOpenDrive(
        IPlacementDbContext db,
        decimal minCgpa = 6.5m,
        int maxBacklogs = 2,
        string? eligibleBranches = null,
        DriveStatus status = DriveStatus.Open,
        DateOnly? deadline = null)
    {
        var tenantId = Guid.NewGuid();
        var company = new Company
        {
            TenantId = tenantId,
            Name = "Test Corp",
            Industry = "IT"
        };
        db.Companies.Add(company);

        var drive = new PlacementDrive
        {
            TenantId = tenantId,
            CompanyId = company.Id,
            CompanyName = company.Name,
            JobRole = "Engineer",
            PackageLpa = 10,
            MinCgpa = minCgpa,
            MaxBacklogs = maxBacklogs,
            EligibleBranches = eligibleBranches,
            Status = status,
            AcademicYear = 2026,
            RegistrationDeadline = deadline
        };
        db.Drives.Add(drive);
        db.SaveChangesAsync().GetAwaiter().GetResult();
        return (company, drive);
    }

    [Fact]
    public async Task Register_BelowMinCgpa_ReturnsFailure()
    {
        var db = CreateContext();
        var (_, drive) = SeedOpenDrive(db, minCgpa: 6.5m);

        var handler = new RegisterForDriveHandler(db);
        var cmd = new RegisterForDriveCommand(drive.TenantId, drive.Id, Guid.NewGuid(), "Alice", 5.0m, 0, "CSE");

        var result = await handler.Handle(cmd, default);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("CGPA below minimum requirement");
    }

    [Fact]
    public async Task Register_ExceedsMaxBacklogs_ReturnsFailure()
    {
        var db = CreateContext();
        var (_, drive) = SeedOpenDrive(db, maxBacklogs: 2);

        var handler = new RegisterForDriveHandler(db);
        var cmd = new RegisterForDriveCommand(drive.TenantId, drive.Id, Guid.NewGuid(), "Bob", 8.0m, 3, "CSE");

        var result = await handler.Handle(cmd, default);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Exceeds maximum allowed backlogs");
    }

    [Fact]
    public async Task Register_IneligibleBranch_ReturnsFailure()
    {
        var db = CreateContext();
        var (_, drive) = SeedOpenDrive(db, eligibleBranches: "CSE,ECE");

        var handler = new RegisterForDriveHandler(db);
        var cmd = new RegisterForDriveCommand(drive.TenantId, drive.Id, Guid.NewGuid(), "Carol", 8.0m, 0, "MBA");

        var result = await handler.Handle(cmd, default);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Branch not eligible for this drive");
    }

    [Fact]
    public async Task Register_ClosedDrive_ReturnsFailure()
    {
        var db = CreateContext();
        var (_, drive) = SeedOpenDrive(db, status: DriveStatus.Completed);

        var handler = new RegisterForDriveHandler(db);
        var cmd = new RegisterForDriveCommand(drive.TenantId, drive.Id, Guid.NewGuid(), "Dave", 8.0m, 0, "CSE");

        var result = await handler.Handle(cmd, default);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not open for registration");
    }

    [Fact]
    public async Task Register_Valid_IncrementsRegistrationCount()
    {
        var db = CreateContext();
        var (_, drive) = SeedOpenDrive(db);

        var handler = new RegisterForDriveHandler(db);
        var cmd = new RegisterForDriveCommand(drive.TenantId, drive.Id, Guid.NewGuid(), "Eve", 8.0m, 0, "CSE");

        var result = await handler.Handle(cmd, default);

        result.IsSuccess.Should().BeTrue();
        drive.TotalRegistrations.Should().Be(1);
    }

    [Fact]
    public async Task SelectStudent_CreatesOffer_UpdatesCompanyStats()
    {
        var db = CreateContext();
        var (company, drive) = SeedOpenDrive(db);

        // Register the student first
        var registration = new DriveRegistration
        {
            TenantId = drive.TenantId,
            DriveId = drive.Id,
            StudentId = Guid.NewGuid(),
            StudentName = "Frank",
            StudentCgpa = 8.0m,
            ActiveBacklogs = 0,
            Branch = "CSE",
            RegisteredAt = DateTime.UtcNow,
            Status = RegistrationStatus.InterviewScheduled
        };
        db.Registrations.Add(registration);
        await db.SaveChangesAsync();

        var handler = new UpdateRegistrationStatusHandler(db);
        var cmd = new UpdateRegistrationStatusCommand(
            drive.TenantId, registration.Id, RegistrationStatus.Selected, null, null, 12.5m);

        var result = await handler.Handle(cmd, default);

        result.IsSuccess.Should().BeTrue();
        var offer = await db.Offers.FirstOrDefaultAsync(x => x.RegistrationId == registration.Id);
        offer.Should().NotBeNull();
        company.TotalOffers.Should().Be(1);
        company.HighestPackageLpa.Should().Be(12.5m);
    }

    [Fact]
    public async Task ConfirmOffer_Accept_SetsAccepted()
    {
        var db = CreateContext();
        var (_, drive) = SeedOpenDrive(db);

        var registration = new DriveRegistration
        {
            TenantId = drive.TenantId,
            DriveId = drive.Id,
            StudentId = Guid.NewGuid(),
            StudentName = "Grace",
            StudentCgpa = 8.0m,
            ActiveBacklogs = 0,
            Branch = "CSE",
            RegisteredAt = DateTime.UtcNow,
            Status = RegistrationStatus.Selected
        };
        db.Registrations.Add(registration);

        var offer = new PlacementOffer
        {
            TenantId = drive.TenantId,
            RegistrationId = registration.Id,
            DriveId = drive.Id,
            StudentId = registration.StudentId,
            CompanyName = drive.CompanyName,
            JobRole = drive.JobRole,
            OfferedPackageLpa = 10m,
            Status = OfferStatus.Issued,
            IssuedAt = DateTime.UtcNow
        };
        db.Offers.Add(offer);
        await db.SaveChangesAsync();

        var handler = new ConfirmOfferHandler(db);
        var result = await handler.Handle(new ConfirmOfferCommand(offer.Id, Accept: true), default);

        result.IsSuccess.Should().BeTrue();
        offer.Status.Should().Be(OfferStatus.Accepted);
        registration.Status.Should().Be(RegistrationStatus.OfferConfirmed);
        offer.ConfirmedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ConfirmOffer_Decline_SetsDeclined()
    {
        var db = CreateContext();
        var (_, drive) = SeedOpenDrive(db);

        var registration = new DriveRegistration
        {
            TenantId = drive.TenantId,
            DriveId = drive.Id,
            StudentId = Guid.NewGuid(),
            StudentName = "Hank",
            StudentCgpa = 8.0m,
            ActiveBacklogs = 0,
            Branch = "CSE",
            RegisteredAt = DateTime.UtcNow,
            Status = RegistrationStatus.Selected
        };
        db.Registrations.Add(registration);

        var offer = new PlacementOffer
        {
            TenantId = drive.TenantId,
            RegistrationId = registration.Id,
            DriveId = drive.Id,
            StudentId = registration.StudentId,
            CompanyName = drive.CompanyName,
            JobRole = drive.JobRole,
            OfferedPackageLpa = 10m,
            Status = OfferStatus.Issued,
            IssuedAt = DateTime.UtcNow
        };
        db.Offers.Add(offer);
        await db.SaveChangesAsync();

        var handler = new ConfirmOfferHandler(db);
        var result = await handler.Handle(new ConfirmOfferCommand(offer.Id, Accept: false), default);

        result.IsSuccess.Should().BeTrue();
        offer.Status.Should().Be(OfferStatus.Declined);
        registration.Status.Should().Be(RegistrationStatus.Withdrew);
    }
}
