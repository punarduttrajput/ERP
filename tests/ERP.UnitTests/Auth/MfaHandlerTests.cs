using ERP.Auth.Application.Commands;
using ERP.Auth.Application.Services;
using ERP.Auth.Domain;
using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Contracts;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ERP.UnitTests.Auth;

public class MfaHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid UserId   = Guid.NewGuid();

    // ── EnableMfaHandler ──────────────────────────────────────────────────────

    [Fact]
    public async Task EnableMfa_UserNotFound_ReturnsFailure()
    {
        var (db, _, _, cache, config) = BuildEnableDeps(user: null);
        var handler = new EnableMfaHandler(db.Object, new Mock<ITotpService>().Object, cache.Object, config);

        var result = await handler.Handle(new EnableMfaCommand(UserId), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task EnableMfa_AlreadyEnabled_ReturnsFailure()
    {
        var user = BuildUser(mfaEnabled: true);
        var (db, _, _, cache, config) = BuildEnableDeps(user);
        var handler = new EnableMfaHandler(db.Object, new Mock<ITotpService>().Object, cache.Object, config);

        var result = await handler.Handle(new EnableMfaCommand(UserId), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("already enabled");
    }

    [Fact]
    public async Task EnableMfa_ValidUser_StoresPendingSecretAndReturnsQrUri()
    {
        var user = BuildUser();
        var (db, totp, _, cache, config) = BuildEnableDeps(user);
        totp.Setup(t => t.GenerateSecret()).Returns("TESTSECRET");
        totp.Setup(t => t.GetQrCodeUri("TESTSECRET", user.Email, It.IsAny<string>()))
            .Returns("otpauth://totp/...");
        cache.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new EnableMfaHandler(db.Object, totp.Object, cache.Object, config);
        var result  = await handler.Handle(new EnableMfaCommand(UserId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Secret.Should().Be("TESTSECRET");
        result.Value.QrCodeUri.Should().Be("otpauth://totp/...");
        cache.Verify(c => c.SetAsync($"mfa_pending:{UserId}", "TESTSECRET", TimeSpan.FromMinutes(10), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── ConfirmMfaHandler ─────────────────────────────────────────────────────

    [Fact]
    public async Task ConfirmMfa_PendingSecretExpired_ReturnsFailure()
    {
        var user = BuildUser();
        var db    = BuildDbWithUser(user);
        var totp  = new Mock<ITotpService>();
        var cache = new Mock<ICacheService>();
        cache.Setup(c => c.GetAsync<string>($"mfa_pending:{UserId}", It.IsAny<CancellationToken>()))
             .ReturnsAsync((string?)null);

        var handler = new ConfirmMfaHandler(db.Object, totp.Object, cache.Object);
        var result  = await handler.Handle(new ConfirmMfaCommand(UserId, "123456"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("expired");
    }

    [Fact]
    public async Task ConfirmMfa_InvalidTotpCode_ReturnsFailure()
    {
        var user = BuildUser();
        var db    = BuildDbWithUser(user);
        var totp  = new Mock<ITotpService>();
        var cache = new Mock<ICacheService>();
        cache.Setup(c => c.GetAsync<string>($"mfa_pending:{UserId}", It.IsAny<CancellationToken>()))
             .ReturnsAsync("TESTSECRET");
        totp.Setup(t => t.Verify("TESTSECRET", "000000")).Returns(false);

        var handler = new ConfirmMfaHandler(db.Object, totp.Object, cache.Object);
        var result  = await handler.Handle(new ConfirmMfaCommand(UserId, "000000"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Invalid TOTP");
    }

    [Fact]
    public async Task ConfirmMfa_ValidCode_EnablesMfaAndReturnsRecoveryCodes()
    {
        var user = BuildUser();
        var db    = BuildDbWithUser(user);
        var totp  = new Mock<ITotpService>();
        var cache = new Mock<ICacheService>();
        cache.Setup(c => c.GetAsync<string>($"mfa_pending:{UserId}", It.IsAny<CancellationToken>()))
             .ReturnsAsync("TESTSECRET");
        totp.Setup(t => t.Verify("TESTSECRET", "123456")).Returns(true);
        cache.Setup(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
             .Returns(Task.CompletedTask);
        db.Setup(d => d.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new ConfirmMfaHandler(db.Object, totp.Object, cache.Object);
        var result  = await handler.Handle(new ConfirmMfaCommand(UserId, "123456"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.RecoveryCodes.Should().HaveCount(8);
        user.MfaEnabled.Should().BeTrue();
        user.MfaSecret.Should().Be("TESTSECRET");
        user.MfaRecoveryCodes.Should().NotBeNullOrEmpty();
    }

    // ── DisableMfaHandler ─────────────────────────────────────────────────────

    [Fact]
    public async Task DisableMfa_NotEnabled_ReturnsFailure()
    {
        var user    = BuildUser(mfaEnabled: false);
        var db      = BuildDbWithUser(user);
        var handler = new DisableMfaHandler(db.Object, new Mock<ITotpService>().Object);

        var result = await handler.Handle(new DisableMfaCommand(UserId, "123456"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not enabled");
    }

    [Fact]
    public async Task DisableMfa_InvalidCode_ReturnsFailure()
    {
        var user = BuildUser(mfaEnabled: true, mfaSecret: "SECRET");
        var db   = BuildDbWithUser(user);
        var totp = new Mock<ITotpService>();
        totp.Setup(t => t.Verify("SECRET", "000000")).Returns(false);

        var handler = new DisableMfaHandler(db.Object, totp.Object);
        var result  = await handler.Handle(new DisableMfaCommand(UserId, "000000"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Invalid TOTP");
    }

    [Fact]
    public async Task DisableMfa_ValidCode_ClearsMfaFields()
    {
        var user = BuildUser(mfaEnabled: true, mfaSecret: "SECRET");
        var db   = BuildDbWithUser(user);
        var totp = new Mock<ITotpService>();
        totp.Setup(t => t.Verify("SECRET", "123456")).Returns(true);
        db.Setup(d => d.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new DisableMfaHandler(db.Object, totp.Object);
        var result  = await handler.Handle(new DisableMfaCommand(UserId, "123456"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        user.MfaEnabled.Should().BeFalse();
        user.MfaSecret.Should().BeNull();
        user.MfaRecoveryCodes.Should().BeNull();
    }

    // ── VerifyMfaLoginHandler ─────────────────────────────────────────────────

    [Fact]
    public async Task VerifyMfaLogin_InvalidChallengeToken_ReturnsFailure()
    {
        var jwtService   = new Mock<IJwtService>();
        var totp         = new Mock<ITotpService>();
        var permissions  = new Mock<IPermissionService>();
        var logger       = new Mock<ILogger<VerifyMfaLoginHandler>>();
        var db           = new Mock<IAuthDbContext>();
        jwtService.Setup(j => j.ValidateMfaChallengeToken("bad-token"))
                  .Returns((false, Guid.Empty, Guid.Empty));

        var handler = new VerifyMfaLoginHandler(db.Object, jwtService.Object, totp.Object, permissions.Object, logger.Object);
        var result  = await handler.Handle(new VerifyMfaLoginCommand("bad-token", "123456", null), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Invalid or expired");
    }

    [Fact]
    public async Task VerifyMfaLogin_ValidTotpCode_ReturnsTokens()
    {
        var user        = BuildUser(mfaEnabled: true, mfaSecret: "SECRET");
        var jwtService  = new Mock<IJwtService>();
        var totp        = new Mock<ITotpService>();
        var permissions = new Mock<IPermissionService>();
        var logger      = new Mock<ILogger<VerifyMfaLoginHandler>>();
        var db          = BuildDbWithUser(user);

        jwtService.Setup(j => j.ValidateMfaChallengeToken("challenge"))
                  .Returns((true, UserId, TenantId));
        totp.Setup(t => t.Verify("SECRET", "123456")).Returns(true);
        permissions.Setup(p => p.GetUserPermissionsAsync(TenantId, UserId, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(new UserPermissions());
        jwtService.Setup(j => j.GenerateAccessToken(It.IsAny<User>(), It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>()))
                  .Returns("access-token");
        jwtService.Setup(j => j.GenerateRefreshToken(null)).Returns(("refresh-token", "family-1"));
        db.Setup(d => d.RefreshTokens.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
          .Returns(new ValueTask<Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<RefreshToken>>());
        db.Setup(d => d.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new VerifyMfaLoginHandler(db.Object, jwtService.Object, totp.Object, permissions.Object, logger.Object);
        var result  = await handler.Handle(new VerifyMfaLoginCommand("challenge", "123456", null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.AccessToken.Should().Be("access-token");
        result.Value.MfaRequired.Should().BeFalse();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static User BuildUser(bool mfaEnabled = false, string? mfaSecret = null) => new()
    {
        Id = UserId, TenantId = TenantId,
        Email = "user@uni.com", PasswordHash = "hash",
        IsActive = true, MfaEnabled = mfaEnabled, MfaSecret = mfaSecret,
        RefreshTokens = new List<RefreshToken>()
    };

    private static Mock<IAuthDbContext> BuildDbWithUser(User? user)
    {
        var db    = new Mock<IAuthDbContext>();
        var users = user is not null
            ? new List<User> { user }.AsQueryable()
            : Enumerable.Empty<User>().AsQueryable();
        var set = CreateMockDbSet(users);
        db.Setup(d => d.Users).Returns(set.Object);
        return db;
    }

    private static (Mock<IAuthDbContext> db, Mock<ITotpService> totp, Mock<IJwtService> jwt,
                    Mock<ICacheService> cache, IConfiguration config) BuildEnableDeps(User? user)
    {
        var db   = BuildDbWithUser(user);
        var totp = new Mock<ITotpService>();
        var jwt  = new Mock<IJwtService>();
        var cache = new Mock<ICacheService>();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:Issuer"] = "ERP Test" })
            .Build();
        return (db, totp, jwt, cache, config);
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
