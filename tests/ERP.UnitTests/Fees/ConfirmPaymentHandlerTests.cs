using System.Security.Cryptography;
using System.Text;
using ERP.Fees.Application.Commands;
using ERP.Fees.Application.Events;
using ERP.Fees.Domain;
using ERP.Fees.Infrastructure;
using ERP.Shared.Application.Abstractions;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace ERP.UnitTests.Fees;

public class ConfirmPaymentHandlerTests
{
    private const string GatewaySecret = "test-secret";

    private static string ComputeSignature(string orderId, string paymentId)
        => Convert.ToHexString(
            HMACSHA256.HashData(
                Encoding.UTF8.GetBytes(GatewaySecret),
                Encoding.UTF8.GetBytes($"{orderId}|{paymentId}")));

    private static IFeesDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<TestFeesDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new TestFeesDbContext(options);
    }

    private static IConfiguration CreateConfig()
    {
        var dict = new Dictionary<string, string?> { ["PaymentGateway:Secret"] = GatewaySecret };
        return new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
    }

    [Fact]
    public async Task InvalidSignature_ReturnsFailure()
    {
        var db = CreateInMemoryDb();
        var tenant = new Mock<ICurrentTenant>();
        tenant.Setup(t => t.TenantId).Returns(Guid.NewGuid());
        var publisher = new Mock<IPublisher>();
        var handler = new ConfirmPaymentHandler(db, tenant.Object, CreateConfig(), publisher.Object);

        var result = await handler.Handle(new ConfirmPaymentCommand("ORD-1", "PAY-1", "wrong-signature", 100), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("signature");
        publisher.Verify(p => p.Publish(It.IsAny<FeePaymentReceivedEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ValidPayment_SetsPaidAndPublishesEvent()
    {
        var db = CreateInMemoryDb();
        var tenantId = Guid.NewGuid();
        var tenant = new Mock<ICurrentTenant>();
        tenant.Setup(t => t.TenantId).Returns(tenantId);
        var publisher = new Mock<IPublisher>();

        var account = new StudentFeeAccount
        {
            TenantId = tenantId,
            StudentId = Guid.NewGuid(),
            FeeStructureId = Guid.NewGuid(),
            AcademicYear = 2026,
            SemesterNumber = 1,
            TotalAmount = 10000,
            NetAmount = 10000,
            DueAmount = 10000
        };
        db.StudentFeeAccounts.Add(account);

        var orderId = "ORD-TEST-001";
        var paymentId = "PAY-TEST-001";
        var payment = new FeePayment
        {
            TenantId = tenantId,
            AccountId = account.Id,
            GatewayOrderId = orderId,
            Amount = 5000,
            Status = PaymentStatus.Initiated
        };
        db.FeePayments.Add(payment);
        await db.SaveChangesAsync();

        var signature = ComputeSignature(orderId, paymentId);
        var handler = new ConfirmPaymentHandler(db, tenant.Object, CreateConfig(), publisher.Object);

        var result = await handler.Handle(new ConfirmPaymentCommand(orderId, paymentId, signature, 5000), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        payment.Status.Should().Be(PaymentStatus.Paid);
        payment.GatewayPaymentId.Should().Be(paymentId);
        payment.ReceiptNumber.Should().NotBeNullOrEmpty();
        publisher.Verify(p => p.Publish(It.IsAny<FeePaymentReceivedEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ValidPayment_UpdatesAccountDueAmount()
    {
        var db = CreateInMemoryDb();
        var tenantId = Guid.NewGuid();
        var tenant = new Mock<ICurrentTenant>();
        tenant.Setup(t => t.TenantId).Returns(tenantId);
        var publisher = new Mock<IPublisher>();

        var account = new StudentFeeAccount
        {
            TenantId = tenantId,
            StudentId = Guid.NewGuid(),
            FeeStructureId = Guid.NewGuid(),
            AcademicYear = 2026,
            SemesterNumber = 1,
            TotalAmount = 10000,
            NetAmount = 10000,
            DueAmount = 10000,
            PaidAmount = 0
        };
        db.StudentFeeAccounts.Add(account);

        var orderId = "ORD-DUE-001";
        var paymentId = "PAY-DUE-001";
        var payment = new FeePayment
        {
            TenantId = tenantId,
            AccountId = account.Id,
            GatewayOrderId = orderId,
            Amount = 10000,
            Status = PaymentStatus.Initiated
        };
        db.FeePayments.Add(payment);
        await db.SaveChangesAsync();

        var signature = ComputeSignature(orderId, paymentId);
        var handler = new ConfirmPaymentHandler(db, tenant.Object, CreateConfig(), publisher.Object);

        await handler.Handle(new ConfirmPaymentCommand(orderId, paymentId, signature, 10000), CancellationToken.None);

        account.PaidAmount.Should().Be(10000);
        account.DueAmount.Should().Be(0);
        account.IsFullyPaid.Should().BeTrue();
    }
}

// In-memory test double for IFeesDbContext
internal class TestFeesDbContext : DbContext, IFeesDbContext
{
    public TestFeesDbContext(DbContextOptions options) : base(options) { }

    public DbSet<FeeStructure> FeeStructures => Set<FeeStructure>();
    public DbSet<FeeComponent> FeeComponents => Set<FeeComponent>();
    public DbSet<InstallmentSchedule> InstallmentSchedules => Set<InstallmentSchedule>();
    public DbSet<StudentFeeAccount> StudentFeeAccounts => Set<StudentFeeAccount>();
    public DbSet<FeeInstallment> FeeInstallments => Set<FeeInstallment>();
    public DbSet<FeePayment> FeePayments => Set<FeePayment>();
    public DbSet<Scholarship> Scholarships => Set<Scholarship>();
    public DbSet<StudentScholarship> StudentScholarships => Set<StudentScholarship>();
    public DbSet<RefundRequest> RefundRequests => Set<RefundRequest>();
}
