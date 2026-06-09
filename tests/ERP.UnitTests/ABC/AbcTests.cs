using ERP.ABC.Application.Commands;
using ERP.ABC.Application.Queries;
using ERP.ABC.Domain;
using ERP.ABC.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace ERP.UnitTests.ABC;

public class AbcTests
{
    private static IAbcDbContext BuildContext()
    {
        var options = new DbContextOptionsBuilder<AbcTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AbcTestDbContext(options);
    }

    [Fact]
    public async Task LinkAbcId_Valid12Digits_Verifies()
    {
        var db = BuildContext();
        var tenantId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var digiLocker = new NullDigiLockerService();

        var handler = new LinkAbcIdHandler(db, digiLocker);
        var result = await handler.Handle(new LinkAbcIdCommand(tenantId, studentId, "123456789012"), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("Test Student (Dev)");

        var profile = db.StudentAbcProfiles.First(x => x.StudentId == studentId);
        profile.IsVerified.Should().BeTrue();
        profile.RegistryStudentName.Should().Be("Test Student (Dev)");
        profile.VerifiedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task LinkAbcId_NotNumeric_ReturnsFailure()
    {
        var db = BuildContext();
        var digiLocker = new NullDigiLockerService();

        var handler = new LinkAbcIdHandler(db, digiLocker);
        var result = await handler.Handle(new LinkAbcIdCommand(Guid.NewGuid(), Guid.NewGuid(), "ABC123456789"), default);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("invalid format");
    }

    [Fact]
    public async Task LinkAbcId_DigiLockerInvalid_ReturnsFailure()
    {
        var db = BuildContext();
        var mockDigiLocker = new Mock<IDigiLockerService>();
        mockDigiLocker
            .Setup(x => x.VerifyAbcIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, (string?)null));

        var handler = new LinkAbcIdHandler(db, mockDigiLocker.Object);
        var result = await handler.Handle(new LinkAbcIdCommand(Guid.NewGuid(), Guid.NewGuid(), "123456789012"), default);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("DigiLocker");
    }

    [Fact]
    public async Task PathwayEligibility_120Credits_DegreeEligible()
    {
        var db = BuildContext();
        var tenantId = Guid.NewGuid();
        var studentId = Guid.NewGuid();

        db.StudentAbcProfiles.Add(new StudentAbcProfile
        {
            TenantId = tenantId,
            StudentId = studentId,
            AbcId = "111111111111",
            IsVerified = true,
            TotalCreditsEarned = 120,
            TotalCreditsTransferredIn = 0
        });
        await db.SaveChangesAsync();

        var handler = new GetPathwayEligibilityHandler(db);
        var result = await handler.Handle(new GetPathwayEligibilityQuery(tenantId, studentId), default);

        result.IsSuccess.Should().BeTrue();
        var degree = result.Value!.EligiblePathways.First(p => p.Type == PathwayType.Degree);
        degree.IsEligible.Should().BeTrue();
        degree.CreditsShortfall.Should().Be(0);
    }

    [Fact]
    public async Task PathwayEligibility_60Credits_DiplomaEligibleCertEligibleDegreeNot()
    {
        var db = BuildContext();
        var tenantId = Guid.NewGuid();
        var studentId = Guid.NewGuid();

        // 60 credits: Certificate (40) eligible, PgDiploma (60) eligible, Diploma (80) not eligible, Degree (120) not eligible
        db.StudentAbcProfiles.Add(new StudentAbcProfile
        {
            TenantId = tenantId,
            StudentId = studentId,
            AbcId = "222222222222",
            IsVerified = true,
            TotalCreditsEarned = 60,
            TotalCreditsTransferredIn = 0
        });
        await db.SaveChangesAsync();

        var handler = new GetPathwayEligibilityHandler(db);
        var result = await handler.Handle(new GetPathwayEligibilityQuery(tenantId, studentId), default);

        result.IsSuccess.Should().BeTrue();
        result.Value!.EligiblePathways.First(p => p.Type == PathwayType.Certificate).IsEligible.Should().BeTrue();
        // Diploma requires 80 credits; 60 < 80 so not eligible
        result.Value!.EligiblePathways.First(p => p.Type == PathwayType.Diploma).IsEligible.Should().BeFalse();
        result.Value!.EligiblePathways.First(p => p.Type == PathwayType.Degree).IsEligible.Should().BeFalse();
    }

    [Fact]
    public async Task ApproveCreditTransfer_IncrementsTotalCreditsIn()
    {
        var db = BuildContext();
        var tenantId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var approverId = Guid.NewGuid();

        var profile = new StudentAbcProfile
        {
            TenantId = tenantId,
            StudentId = studentId,
            AbcId = "333333333333",
            IsVerified = true,
            TotalCreditsTransferredIn = 5
        };
        db.StudentAbcProfiles.Add(profile);

        var transfer = new CreditTransfer
        {
            TenantId = tenantId,
            StudentId = studentId,
            AbcId = "333333333333",
            Direction = TransferDirection.Incoming,
            SourceInstitution = "IIT Delhi",
            SubjectCode = "CS101",
            SubjectName = "Data Structures",
            CreditsRequested = 10,
            AcademicYear = 2025,
            Status = TransferStatus.Pending
        };
        db.CreditTransfers.Add(transfer);
        await db.SaveChangesAsync();

        var handler = new ApproveCreditTransferHandler(db);
        var result = await handler.Handle(new ApproveCreditTransferCommand(tenantId, transfer.Id, 10, approverId, "ABC-REF-001"), default);

        result.IsSuccess.Should().BeTrue();
        var updatedProfile = db.StudentAbcProfiles.First(x => x.StudentId == studentId);
        updatedProfile.TotalCreditsTransferredIn.Should().Be(15);
    }

    [Fact]
    public async Task ChoosePathway_InsufficientCredits_ReturnsFailure()
    {
        var db = BuildContext();
        var tenantId = Guid.NewGuid();
        var studentId = Guid.NewGuid();

        db.StudentAbcProfiles.Add(new StudentAbcProfile
        {
            TenantId = tenantId,
            StudentId = studentId,
            AbcId = "444444444444",
            IsVerified = true,
            TotalCreditsEarned = 30,
            TotalCreditsTransferredIn = 0
        });
        await db.SaveChangesAsync();

        var handler = new ChoosePathwayHandler(db);
        var result = await handler.Handle(new ChoosePathwayCommand(tenantId, studentId, PathwayType.Diploma), default);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Insufficient");
    }
}

internal class AbcTestDbContext : DbContext, IAbcDbContext
{
    public AbcTestDbContext(DbContextOptions<AbcTestDbContext> options) : base(options) { }
    public DbSet<StudentAbcProfile> StudentAbcProfiles => Set<StudentAbcProfile>();
    public DbSet<CreditTransfer> CreditTransfers => Set<CreditTransfer>();
    public DbSet<AcademicPathway> AcademicPathways => Set<AcademicPathway>();
}
