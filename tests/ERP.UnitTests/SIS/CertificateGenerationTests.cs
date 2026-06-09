using ERP.SIS.Application.Commands;
using ERP.SIS.Domain;
using ERP.SIS.Infrastructure;
using ERP.Shared.Application.Abstractions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace ERP.UnitTests.SIS;

public class CertificateGenerationTests
{
    private static ISisDbContext BuildDb(Student? student = null)
    {
        var options = new DbContextOptionsBuilder<SisTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var ctx = new SisTestDbContext(options);
        if (student is not null)
            ctx.Students.Add(student);
        ctx.SaveChanges();
        return ctx;
    }

    private static Student SampleStudent(Guid tenantId) => new()
    {
        Id = Guid.NewGuid(),
        TenantId = tenantId,
        StudentNumber = "2026-ABCD-00001",
        ApplicationId = Guid.NewGuid(),
        ProgramId = Guid.NewGuid(),
        ProgramName = "B.Tech CSE",
        AcademicYear = 2026,
        EnrolledAt = DateTime.UtcNow,
        FirstName = "Priya",
        LastName = "Sharma",
        Email = "priya@example.com",
        MobileNumber = "9000000001",
        DateOfBirth = new DateOnly(2003, 5, 12),
        Gender = "Female",
        Category = "General"
    };

    private static IPdfService FakePdf()
    {
        var mock = new Mock<IPdfService>();
        // Return non-empty bytes so the handler result has content.
        mock.Setup(p => p.GeneratePdfAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new byte[] { 0x25, 0x50, 0x44, 0x46 }); // %PDF
        return mock.Object;
    }

    [Theory]
    [InlineData(CertificateType.Bonafide)]
    [InlineData(CertificateType.Character)]
    [InlineData(CertificateType.Provisional)]
    public async Task GenerateCertificate_ValidStudent_ReturnsNonEmptyBytes(CertificateType certType)
    {
        var tenantId = Guid.NewGuid();
        var student = SampleStudent(tenantId);
        var db = BuildDb(student);
        var handler = new GenerateCertificateHandler(db, FakePdf());

        var result = await handler.Handle(new GenerateCertificateCommand(student.Id, certType), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GenerateCertificate_StudentNotFound_ReturnsFailure()
    {
        var db = BuildDb();
        var handler = new GenerateCertificateHandler(db, FakePdf());

        var result = await handler.Handle(new GenerateCertificateCommand(Guid.NewGuid(), CertificateType.Bonafide), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not found");
    }
}
