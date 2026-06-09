using ERP.Auth.Application.Commands;
using ERP.Auth.Application.Services;
using ERP.Auth.Domain;
using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Contracts;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ERP.UnitTests.Auth;

public class OtpHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid UserId   = Guid.NewGuid();

    // ── SendOtpHandler ────────────────────────────────────────────────────────

    [Fact]
    public async Task SendOtp_NoTenantContext_ReturnsFailure()
    {
        var tenant = new Mock<ICurrentTenant>();
        tenant.Setup(t => t.TenantId).Returns((Guid?)null);
        var handler = BuildSendHandler(tenant: tenant);

        var result = await handler.Handle(new SendOtpCommand("+911234567890"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Tenant context");
    }

    [Fact]
    public async Task SendOtp_ValidRequest_StoresHashedOtpAndSendsWhatsApp()
    {
        var whatsApp = new Mock<IWhatsAppService>();
        var cache    = new Mock<ICacheService>();
        whatsApp.Setup(s => s.SendOtpAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
        cache.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
             .Returns(Task.CompletedTask);

        var handler = BuildSendHandler(whatsApp: whatsApp, cache: cache);
        var result  = await handler.Handle(new SendOtpCommand("+911234567890"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        whatsApp.Verify(s => s.SendOtpAsync("+911234567890", It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        cache.Verify(c => c.SetAsync(
            It.Is<string>(k => k.Contains("+911234567890")),
            It.IsAny<string>(),
            TimeSpan.FromMinutes(5),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── VerifyOtpHandler ──────────────────────────────────────────────────────

    [Fact]
    public async Task VerifyOtp_NotFoundOrExpired_ReturnsFailure()
    {
        var cache = new Mock<ICacheService>();
        cache.Setup(c => c.GetAsync<string>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync((string?)null);
        var handler = BuildVerifyHandler(cache: cache);

        var result = await handler.Handle(new VerifyOtpCommand("+911234567890", "123456", null), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("expired");
    }

    [Fact]
    public async Task VerifyOtp_WrongCode_ReturnsFailure()
    {
        var correctHash = System.Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes("999999")));

        var cache = new Mock<ICacheService>();
        cache.Setup(c => c.GetAsync<string>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(correctHash);
        var handler = BuildVerifyHandler(cache: cache);

        var result = await handler.Handle(new VerifyOtpCommand("+911234567890", "123456", null), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Invalid OTP");
    }

    [Fact]
    public async Task VerifyOtp_CorrectCode_ReturnsTokensAndInvalidatesCache()
    {
        const string otp = "123456";
        var hash = System.Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(otp)));

        var cache = new Mock<ICacheService>();
        cache.Setup(c => c.GetAsync<string>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(hash);
        cache.Setup(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
             .Returns(Task.CompletedTask);

        var jwtService  = new Mock<IJwtService>();
        var permissions = new Mock<IPermissionService>();
        var db          = BuildDbWithUser();
        jwtService.Setup(j => j.GenerateAccessToken(It.IsAny<User>(), It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>()))
                  .Returns("access");
        jwtService.Setup(j => j.GenerateRefreshToken(null)).Returns(("refresh", "fam"));
        permissions.Setup(p => p.GetUserPermissionsAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(new UserPermissions());
        db.Setup(d => d.RefreshTokens.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
          .Returns(new ValueTask<Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<RefreshToken>>());
        db.Setup(d => d.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = BuildVerifyHandler(cache: cache, db: db, jwtService: jwtService, permissions: permissions);
        var result  = await handler.Handle(new VerifyOtpCommand("+911234567890", otp, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.AccessToken.Should().Be("access");
        cache.Verify(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static SendOtpHandler BuildSendHandler(
        Mock<IWhatsAppService>? whatsApp = null,
        Mock<ICacheService>? cache = null,
        Mock<ICurrentTenant>? tenant = null)
    {
        var db = new Mock<IAuthDbContext>();
        var users = new List<User> { BuildUser() }.AsQueryable();
        var set = CreateMockDbSet(users);
        db.Setup(d => d.Users).Returns(set.Object);

        if (tenant is null)
        {
            tenant = new Mock<ICurrentTenant>();
            tenant.Setup(t => t.TenantId).Returns(TenantId);
        }
        return new SendOtpHandler(
            db.Object,
            cache?.Object ?? new Mock<ICacheService>().Object,
            whatsApp?.Object ?? new Mock<IWhatsAppService>().Object,
            tenant.Object,
            new Mock<ILogger<SendOtpHandler>>().Object);
    }

    private static VerifyOtpHandler BuildVerifyHandler(
        Mock<ICacheService>? cache = null,
        Mock<IAuthDbContext>? db = null,
        Mock<IJwtService>? jwtService = null,
        Mock<IPermissionService>? permissions = null)
    {
        db ??= BuildDbWithUser();
        var tenant = new Mock<ICurrentTenant>();
        tenant.Setup(t => t.TenantId).Returns(TenantId);
        return new VerifyOtpHandler(
            db.Object,
            cache?.Object ?? new Mock<ICacheService>().Object,
            jwtService?.Object ?? new Mock<IJwtService>().Object,
            permissions?.Object ?? new Mock<IPermissionService>().Object,
            tenant.Object,
            new Mock<ILogger<VerifyOtpHandler>>().Object);
    }

    private static User BuildUser() => new()
    {
        Id = UserId, TenantId = TenantId,
        Email = "user@uni.com", PasswordHash = "hash", IsActive = true,
        MobileNumber = "+911234567890",
        RefreshTokens = new List<RefreshToken>()
    };

    private static Mock<IAuthDbContext> BuildDbWithUser()
    {
        var db  = new Mock<IAuthDbContext>();
        var set = CreateMockDbSet(new List<User> { BuildUser() }.AsQueryable());
        db.Setup(d => d.Users).Returns(set.Object);
        return db;
    }

    private static Mock<DbSet<T>> CreateMockDbSet<T>(IQueryable<T> data) where T : class
    {
        var mock = new Mock<DbSet<T>>();
        mock.As<IAsyncEnumerable<T>>()
            .Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
            .Returns(new TestAsyncEnumerator<T>(data.GetEnumerator()));
        mock.As<IQueryable<T>>().Setup(m => m.Provider).Returns(new TestAsyncQueryProvider<T>(data.Provider));
        mock.As<IQueryable<T>>().Setup(m => m.Expression).Returns(data.Expression);
        mock.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(data.ElementType);
        mock.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());
        return mock;
    }
}
