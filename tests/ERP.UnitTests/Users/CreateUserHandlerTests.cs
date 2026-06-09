using ERP.Auth.Domain;
using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Common;
using ERP.Users.Application.Commands;
using ERP.Users.Domain;
using ERP.Users.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using ERP.UnitTests.Auth; // TestAsyncQueryProvider, TestAsyncEnumerator

namespace ERP.UnitTests.Users;

public class CreateUserHandlerTests
{
    private readonly Mock<IUsersDbContext> _dbMock = new();
    private readonly Mock<ICurrentTenant> _tenantMock = new();
    private readonly Mock<ICurrentUser> _userMock = new();
    private readonly Mock<ILogger<CreateUserHandler>> _loggerMock = new();

    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid ActorId  = Guid.NewGuid();

    private readonly CreateUserHandler _handler;

    public CreateUserHandlerTests()
    {
        _tenantMock.Setup(t => t.TenantId).Returns(TenantId);
        _userMock.Setup(u => u.UserId).Returns(ActorId);

        _dbMock.Setup(d => d.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _handler = new CreateUserHandler(
            _dbMock.Object,
            _tenantMock.Object,
            _userMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_NoTenantContext_ReturnsFailure()
    {
        _tenantMock.Setup(t => t.TenantId).Returns((Guid?)null);

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Tenant context");
    }

    [Fact]
    public async Task Handle_DuplicateEmail_ReturnsFailure()
    {
        SetupUsersDbSet(existingEmail: "john@uni.com");

        var result = await _handler.Handle(ValidCommand("john@uni.com"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("already registered");
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesUserAndProfileAndReturnsGuid()
    {
        SetupUsersDbSet();
        SetupUserProfilesDbSet();

        _dbMock.Setup(d => d.Users.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<User>>());
        _dbMock.Setup(d => d.UserProfiles.AddAsync(It.IsAny<UserProfile>(), It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<UserProfile>>());

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBe(Guid.Empty);
        _dbMock.Verify(d => d.Users.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
        _dbMock.Verify(d => d.UserProfiles.AddAsync(It.IsAny<UserProfile>(), It.IsAny<CancellationToken>()), Times.Once);
        _dbMock.Verify(d => d.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ValidCommand_HashesPassword()
    {
        SetupUsersDbSet();
        SetupUserProfilesDbSet();

        User? capturedUser = null;
        _dbMock.Setup(d => d.Users.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((u, _) => capturedUser = u)
            .Returns(new ValueTask<Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<User>>());
        _dbMock.Setup(d => d.UserProfiles.AddAsync(It.IsAny<UserProfile>(), It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<UserProfile>>());

        await _handler.Handle(ValidCommand(), CancellationToken.None);

        capturedUser.Should().NotBeNull();
        capturedUser!.PasswordHash.Should().NotBe("Password123!");
        BCrypt.Net.BCrypt.Verify("Password123!", capturedUser.PasswordHash).Should().BeTrue();
    }

    private static CreateUserCommand ValidCommand(string email = "new@uni.com") =>
        new(email, "Password123!", "John", "Doe", "555-1234", "Engineering", "Developer");

    private void SetupUsersDbSet(string? existingEmail = null)
    {
        var users = existingEmail is not null
            ? new List<User> { new() { TenantId = TenantId, Email = existingEmail } }.AsQueryable()
            : Enumerable.Empty<User>().AsQueryable();

        var set = CreateMockDbSet(users);
        _dbMock.Setup(d => d.Users).Returns(set.Object);
    }

    private void SetupUserProfilesDbSet()
    {
        var set = CreateMockDbSet(Enumerable.Empty<UserProfile>().AsQueryable());
        _dbMock.Setup(d => d.UserProfiles).Returns(set.Object);
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
