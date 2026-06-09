using ERP.Alumni.Application.Commands;
using ERP.Alumni.Domain;
using ERP.Alumni.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Moq;
using ERP.Shared.Application.Abstractions;
using Xunit;

namespace ERP.UnitTests.Alumni;

public class AlumniTestDbContext : DbContext, IAlumniDbContext
{
    public AlumniTestDbContext(DbContextOptions<AlumniTestDbContext> options) : base(options) { }

    public DbSet<AlumniProfile> AlumniProfiles => Set<AlumniProfile>();
    public DbSet<AlumniEvent> AlumniEvents => Set<AlumniEvent>();
    public DbSet<EventRegistration> EventRegistrations => Set<EventRegistration>();
    public DbSet<DonationCampaign> DonationCampaigns => Set<DonationCampaign>();
    public DbSet<DonationPledge> DonationPledges => Set<DonationPledge>();
}

public class AlumniTests
{
    private static AlumniTestDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AlumniTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AlumniTestDbContext(options);
    }

    [Fact]
    public async Task RegisterAlumni_DuplicateEmail_ReturnsFailure()
    {
        var db = CreateDb();
        var tenantId = Guid.NewGuid();
        var handler = new RegisterAlumniHandler(db);
        var command = new RegisterAlumniCommand(tenantId, "Jane", "Doe", "jane@example.com", 2020, "CSE", null, null);

        var first = await handler.Handle(command, CancellationToken.None);
        Assert.True(first.IsSuccess);

        var second = await handler.Handle(command, CancellationToken.None);
        Assert.False(second.IsSuccess);
        Assert.Contains("already registered", second.Error);
    }

    [Fact]
    public async Task PledgeDonation_InactiveCampaign_ReturnsFailure()
    {
        var db = CreateDb();
        var tenantId = Guid.NewGuid();
        var alumniId = Guid.NewGuid();

        var profile = new AlumniProfile
        {
            TenantId = tenantId,
            Id = alumniId,
            FirstName = "John",
            LastName = "Smith",
            Email = "john@example.com",
            GraduationYear = 2019,
            ProgramName = "MBA"
        };
        db.AlumniProfiles.Add(profile);

        var campaign = new DonationCampaign
        {
            TenantId = tenantId,
            Title = "Library Fund",
            TargetAmount = 100000,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30)),
            IsActive = false
        };
        db.DonationCampaigns.Add(campaign);
        await db.SaveChangesAsync();

        var handler = new PledgeDonationHandler(db);
        var result = await handler.Handle(new PledgeDonationCommand(tenantId, campaign.Id, alumniId, 5000), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("not active", result.Error);
    }

    [Fact]
    public async Task RecordPayment_PartialAmount_StatusPartiallyPaid()
    {
        var db = CreateDb();
        var tenantId = Guid.NewGuid();

        var campaign = new DonationCampaign
        {
            TenantId = tenantId,
            Title = "Scholarship Fund",
            TargetAmount = 50000,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10)),
            IsActive = true
        };
        db.DonationCampaigns.Add(campaign);

        var pledge = new DonationPledge
        {
            TenantId = tenantId,
            CampaignId = campaign.Id,
            AlumniId = Guid.NewGuid(),
            AlumniName = "Test Alumni",
            AlumniEmail = "test@alumni.com",
            PledgedAmount = 10000,
            PaidAmount = 0,
            Status = PledgeStatus.Pledged,
            PledgedAt = DateTime.UtcNow
        };
        db.DonationPledges.Add(pledge);
        await db.SaveChangesAsync();

        var handler = new RecordDonationPaymentHandler(db);
        var result = await handler.Handle(new RecordDonationPaymentCommand(tenantId, pledge.Id, 5000), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var updated = await db.DonationPledges.FindAsync(pledge.Id);
        Assert.Equal(PledgeStatus.PartiallyPaid, updated!.Status);
        Assert.Equal(5000m, updated.PaidAmount);
    }

    [Fact]
    public async Task RecordPayment_FullAmount_StatusFullyPaid_ReceiptGenerated()
    {
        var db = CreateDb();
        var tenantId = Guid.NewGuid();

        var campaign = new DonationCampaign
        {
            TenantId = tenantId,
            Title = "Infrastructure Fund",
            TargetAmount = 200000,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5)),
            IsActive = true
        };
        db.DonationCampaigns.Add(campaign);

        var pledge = new DonationPledge
        {
            TenantId = tenantId,
            CampaignId = campaign.Id,
            AlumniId = Guid.NewGuid(),
            AlumniName = "Full Payer",
            AlumniEmail = "full@alumni.com",
            PledgedAmount = 10000,
            PaidAmount = 0,
            Status = PledgeStatus.Pledged,
            PledgedAt = DateTime.UtcNow
        };
        db.DonationPledges.Add(pledge);
        await db.SaveChangesAsync();

        var handler = new RecordDonationPaymentHandler(db);
        var result = await handler.Handle(new RecordDonationPaymentCommand(tenantId, pledge.Id, 10000), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var updated = await db.DonationPledges.FindAsync(pledge.Id);
        Assert.Equal(PledgeStatus.FullyPaid, updated!.Status);
        Assert.NotNull(updated.ReceiptNumber);
        Assert.StartsWith("80G-", updated.ReceiptNumber);
    }

    [Fact]
    public async Task GenerateReceipt_CampaignNotEligible_ReturnsFailure()
    {
        var db = CreateDb();
        var tenantId = Guid.NewGuid();

        var campaign = new DonationCampaign
        {
            TenantId = tenantId,
            Title = "Sports Fund",
            TargetAmount = 50000,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10)),
            IsActive = true,
            Section80GEligible = false
        };
        db.DonationCampaigns.Add(campaign);

        var pledge = new DonationPledge
        {
            TenantId = tenantId,
            CampaignId = campaign.Id,
            AlumniId = Guid.NewGuid(),
            AlumniName = "Donor",
            AlumniEmail = "donor@alumni.com",
            PledgedAmount = 5000,
            PaidAmount = 5000,
            Status = PledgeStatus.FullyPaid,
            PledgedAt = DateTime.UtcNow,
            ReceiptNumber = "80G-TEST-2026-ABCDEF"
        };
        db.DonationPledges.Add(pledge);
        await db.SaveChangesAsync();

        var pdfService = new Mock<IPdfService>();
        var handler = new GenerateSection80GReceiptHandler(db, pdfService.Object);
        var result = await handler.Handle(new GenerateSection80GReceiptCommand(tenantId, pledge.Id), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("not eligible", result.Error);
    }

    [Fact]
    public async Task GenerateReceipt_NotFullyPaid_ReturnsFailure()
    {
        var db = CreateDb();
        var tenantId = Guid.NewGuid();

        var campaign = new DonationCampaign
        {
            TenantId = tenantId,
            Title = "Research Fund",
            TargetAmount = 100000,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-15)),
            IsActive = true,
            Section80GEligible = true,
            Section80GRegistrationNumber = "80G/2024/001"
        };
        db.DonationCampaigns.Add(campaign);

        var pledge = new DonationPledge
        {
            TenantId = tenantId,
            CampaignId = campaign.Id,
            AlumniId = Guid.NewGuid(),
            AlumniName = "Partial Donor",
            AlumniEmail = "partial@alumni.com",
            PledgedAmount = 10000,
            PaidAmount = 5000,
            Status = PledgeStatus.PartiallyPaid,
            PledgedAt = DateTime.UtcNow
        };
        db.DonationPledges.Add(pledge);
        await db.SaveChangesAsync();

        var pdfService = new Mock<IPdfService>();
        var handler = new GenerateSection80GReceiptHandler(db, pdfService.Object);
        var result = await handler.Handle(new GenerateSection80GReceiptCommand(tenantId, pledge.Id), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("fully paid", result.Error);
    }
}
