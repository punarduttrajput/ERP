using ERP.Auth.Application.Commands;
using ERP.Auth.Application.Services;
using ERP.Auth.Domain;
using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Contracts;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ERP.UnitTests.Auth;

public class RefreshTokenHandlerTests
{
    private readonly Mock<IAuthDbContext> _dbMock;
    private readonly Mock<IJwtService> _jwtServiceMock;
    private readonly Mock<IPermissionService> _permissionsMock;
    private readonly Mock<ILogger<RefreshTokenHandler>> _loggerMock;
    private readonly RefreshTokenHandler _handler;

    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();

    public RefreshTokenHandlerTests()
    {
        _dbMock = new Mock<IAuthDbContext>();
        _jwtServiceMock = new Mock<IJwtService>();
        _permissionsMock = new Mock<IPermissionService>();
        _loggerMock = new Mock<ILogger<RefreshTokenHandler>>();

        _permissionsMock
            .Setup(p => p.GetUserPermissionsAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserPermissions());

        _dbMock.Setup(d => d.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _handler = new RefreshTokenHandler(
            _dbMock.Object,
            _jwtServiceMock.Object,
            _permissionsMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ValidToken_ReturnsNewTokenPair()
    {
        var token = BuildActiveToken("valid-token", "family-1");
        SetupRefreshTokenDbSet(token);

        _jwtServiceMock.Setup(j => j.GenerateAccessToken(It.IsAny<User>(), It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>()))
            .Returns("new-access-token");
        _jwtServiceMock.Setup(j => j.GenerateRefreshToken("family-1"))
            .Returns(("new-refresh-token", "family-1"));

        _dbMock.Setup(d => d.RefreshTokens.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<EntityEntry<RefreshToken>>());

        var result = await _handler.Handle(new RefreshTokenCommand("valid-token", "127.0.0.1"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.AccessToken.Should().Be("new-access-token");
        result.Value.RefreshToken.Should().Be("new-refresh-token");
        token.IsUsed.Should().BeTrue();
        token.ReplacedByToken.Should().Be("new-refresh-token");
    }

    [Fact]
    public async Task Handle_AlreadyUsedToken_RevokesEntireFamilyAndReturnsFailure()
    {
        // Simulate a stolen token: original was already used (rotated), attacker tries to reuse it
        var usedToken = BuildActiveToken("stolen-token", "family-abc");
        usedToken.IsUsed = true;

        var sibling1 = BuildActiveToken("sibling-token-1", "family-abc");
        var sibling2 = BuildActiveToken("sibling-token-2", "family-abc");

        // First query: find the token being presented
        SetupRefreshTokenDbSet(usedToken);

        // Second query inside RevokeFamilyAsync: find all non-revoked tokens in the family
        var familyTokens = new List<RefreshToken> { sibling1, sibling2 }.AsQueryable();
        var familySet = CreateMockDbSet(familyTokens);
        _dbMock.SetupSequence(d => d.RefreshTokens)
            .Returns(CreateMockDbSet(new List<RefreshToken> { usedToken }.AsQueryable()).Object)
            .Returns(familySet.Object);

        var result = await _handler.Handle(new RefreshTokenCommand("stolen-token", "127.0.0.1"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Token reuse detected");
        sibling1.IsRevoked.Should().BeTrue();
        sibling2.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_RevokedToken_RevokesEntireFamilyAndReturnsFailure()
    {
        var revokedToken = BuildActiveToken("revoked-token", "family-xyz");
        revokedToken.IsRevoked = true;

        var familyMember = BuildActiveToken("other-token", "family-xyz");

        _dbMock.SetupSequence(d => d.RefreshTokens)
            .Returns(CreateMockDbSet(new List<RefreshToken> { revokedToken }.AsQueryable()).Object)
            .Returns(CreateMockDbSet(new List<RefreshToken> { familyMember }.AsQueryable()).Object);

        var result = await _handler.Handle(new RefreshTokenCommand("revoked-token", "127.0.0.1"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Token reuse detected");
        familyMember.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ExpiredToken_ReturnsFailure()
    {
        var expiredToken = BuildActiveToken("expired-token", "family-1");
        expiredToken.ExpiresAt = DateTime.UtcNow.AddMinutes(-1);

        SetupRefreshTokenDbSet(expiredToken);

        var result = await _handler.Handle(new RefreshTokenCommand("expired-token", "127.0.0.1"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("expired");
    }

    [Fact]
    public async Task Handle_UnknownToken_ReturnsFailure()
    {
        SetupRefreshTokenDbSet();

        var result = await _handler.Handle(new RefreshTokenCommand("nonexistent-token", "127.0.0.1"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Invalid refresh token");
    }

    [Fact]
    public async Task Handle_InactiveUser_ReturnsFailure()
    {
        var token = BuildActiveToken("valid-token", "family-1");
        token.User!.IsActive = false;

        SetupRefreshTokenDbSet(token);

        var result = await _handler.Handle(new RefreshTokenCommand("valid-token", "127.0.0.1"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("inactive");
    }

    private static RefreshToken BuildActiveToken(string tokenValue, string familyId) => new()
    {
        Id = Guid.NewGuid(),
        TenantId = TenantId,
        UserId = UserId,
        Token = tokenValue,
        FamilyId = familyId,
        ExpiresAt = DateTime.UtcNow.AddDays(7),
        IsRevoked = false,
        IsUsed = false,
        User = new User
        {
            Id = UserId,
            TenantId = TenantId,
            Email = "user@test.com",
            PasswordHash = "hash",
            IsActive = true,
            RefreshTokens = new List<RefreshToken>()
        }
    };

    private void SetupRefreshTokenDbSet(params RefreshToken[] tokens)
    {
        var set = CreateMockDbSet(tokens.AsQueryable());
        _dbMock.Setup(d => d.RefreshTokens).Returns(set.Object);
    }

    private static Mock<DbSet<T>> CreateMockDbSet<T>(IQueryable<T> data) where T : class
    {
        var mockSet = new Mock<DbSet<T>>();
        mockSet.As<IAsyncEnumerable<T>>()
            .Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
            .Returns(new TestAsyncEnumerator<T>(data.GetEnumerator()));
        mockSet.As<IQueryable<T>>()
            .Setup(m => m.Provider)
            .Returns(new TestAsyncQueryProvider<T>(data.Provider));
        mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(data.Expression);
        mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(data.ElementType);
        mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());
        return mockSet;
    }
}
