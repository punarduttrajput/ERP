using ERP.Auth.Application.Commands;
using ERP.Auth.Application.Services;
using ERP.Auth.Domain;
using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Contracts;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ERP.UnitTests.Auth;

public class LoginHandlerTests
{
    private readonly Mock<IAuthDbContext> _dbMock;
    private readonly Mock<IJwtService> _jwtServiceMock;
    private readonly Mock<IPermissionService> _permissionsMock;
    private readonly Mock<ICurrentTenant> _currentTenantMock;
    private readonly Mock<ILogger<LoginHandler>> _loggerMock;
    private readonly LoginHandler _handler;

    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();

    public LoginHandlerTests()
    {
        _dbMock = new Mock<IAuthDbContext>();
        _jwtServiceMock = new Mock<IJwtService>();
        _permissionsMock = new Mock<IPermissionService>();
        _currentTenantMock = new Mock<ICurrentTenant>();
        _loggerMock = new Mock<ILogger<LoginHandler>>();

        _currentTenantMock.Setup(t => t.TenantId).Returns(TenantId);
        _permissionsMock
            .Setup(p => p.GetUserPermissionsAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserPermissions());

        _handler = new LoginHandler(
            _dbMock.Object,
            _jwtServiceMock.Object,
            _permissionsMock.Object,
            _currentTenantMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCredentials_ReturnsSuccess()
    {
        // Arrange
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("TestPass123!", workFactor: 4);
        var user = CreateTestUser("test@example.com", passwordHash);

        SetupDbMockWithUser(user);
        _jwtServiceMock.Setup(j => j.GenerateAccessToken(It.IsAny<User>(), It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>()))
            .Returns("access-token");
        _jwtServiceMock.Setup(j => j.GenerateRefreshToken(null))
            .Returns(("refresh-token", "family-id"));
        _dbMock.Setup(d => d.RefreshTokens.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<RefreshToken>>());
        _dbMock.Setup(d => d.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new LoginCommand("test@example.com", "TestPass123!", "127.0.0.1");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.AccessToken.Should().Be("access-token");
        result.Value.RefreshToken.Should().Be("refresh-token");
        result.Value.Email.Should().Be("test@example.com");
    }

    [Fact]
    public async Task Handle_InvalidPassword_ReturnsFailure()
    {
        // Arrange
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword!", workFactor: 4);
        var user = CreateTestUser("test@example.com", passwordHash);

        SetupDbMockWithUser(user);
        _dbMock.Setup(d => d.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new LoginCommand("test@example.com", "WrongPassword!", "127.0.0.1");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Invalid credentials.");
    }

    [Fact]
    public async Task Handle_UserNotFound_ReturnsFailure()
    {
        // Arrange
        var users = new List<User>().AsQueryable();
        var mockSet = CreateMockDbSet(users);
        _dbMock.Setup(d => d.Users).Returns(mockSet.Object);

        var command = new LoginCommand("nonexistent@example.com", "Password123!", "127.0.0.1");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Invalid credentials.");
    }

    [Fact]
    public async Task Handle_InactiveUser_ReturnsFailure()
    {
        // Arrange
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("TestPass123!", workFactor: 4);
        var user = CreateTestUser("inactive@example.com", passwordHash);
        user.IsActive = false;

        SetupDbMockWithUser(user);

        var command = new LoginCommand("inactive@example.com", "TestPass123!", "127.0.0.1");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Account is deactivated.");
    }

    [Fact]
    public async Task Handle_LockedOutUser_ReturnsFailure()
    {
        // Arrange
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("TestPass123!", workFactor: 4);
        var user = CreateTestUser("locked@example.com", passwordHash);
        user.LockoutEndAt = DateTime.UtcNow.AddMinutes(10); // still locked

        SetupDbMockWithUser(user);

        var command = new LoginCommand("locked@example.com", "TestPass123!", "127.0.0.1");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("locked");
    }

    private static User CreateTestUser(string email, string passwordHash) => new()
    {
        Id = UserId,
        TenantId = TenantId,
        Email = email,
        PasswordHash = passwordHash,
        IsActive = true,
        RefreshTokens = new List<RefreshToken>()
    };

    private void SetupDbMockWithUser(User user)
    {
        var users = new List<User> { user }.AsQueryable();
        var mockSet = CreateMockDbSet(users);
        _dbMock.Setup(d => d.Users).Returns(mockSet.Object);
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

// Helper classes for async EF Core mocking
internal class TestAsyncQueryProvider<TEntity> : IAsyncQueryProvider
{
    private readonly IQueryProvider _inner;

    internal TestAsyncQueryProvider(IQueryProvider inner) => _inner = inner;

    public IQueryable CreateQuery(System.Linq.Expressions.Expression expression) => new TestAsyncEnumerable<TEntity>(expression);
    public IQueryable<TElement> CreateQuery<TElement>(System.Linq.Expressions.Expression expression) => new TestAsyncEnumerable<TElement>(expression);
    public object? Execute(System.Linq.Expressions.Expression expression) => _inner.Execute(expression);
    public TResult Execute<TResult>(System.Linq.Expressions.Expression expression) => _inner.Execute<TResult>(expression);
    public TResult ExecuteAsync<TResult>(System.Linq.Expressions.Expression expression, CancellationToken cancellationToken = default)
    {
        var resultType = typeof(TResult).GetGenericArguments()[0];
        var executeMethod = typeof(IQueryProvider)
            .GetMethod(nameof(IQueryProvider.Execute))!
            .MakeGenericMethod(resultType);
        var result = executeMethod.Invoke(_inner, new object[] { expression })!;
        return (TResult)typeof(Task).GetMethod(nameof(Task.FromResult))!
            .MakeGenericMethod(resultType)
            .Invoke(null, new[] { result })!;
    }
}

internal class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
{
    public TestAsyncEnumerable(IEnumerable<T> enumerable) : base(enumerable) { }
    public TestAsyncEnumerable(System.Linq.Expressions.Expression expression) : base(expression) { }
    IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken ct = default)
        => new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
}

internal class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
{
    private readonly IEnumerator<T> _inner;
    public TestAsyncEnumerator(IEnumerator<T> inner) => _inner = inner;
    public T Current => _inner.Current;
    public ValueTask DisposeAsync() { _inner.Dispose(); return ValueTask.CompletedTask; }
    public ValueTask<bool> MoveNextAsync() => ValueTask.FromResult(_inner.MoveNext());
}
