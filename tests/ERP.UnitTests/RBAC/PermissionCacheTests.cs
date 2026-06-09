using ERP.RBAC.Domain;
using ERP.RBAC.Infrastructure;
using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Contracts;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ERP.UnitTests.RBAC;

public class PermissionCacheTests
{
    private readonly Mock<ICacheService> _cacheMock;
    private readonly Mock<IRbacDbContext> _dbMock;
    private readonly Mock<ILogger<PermissionCacheService>> _loggerMock;
    private readonly PermissionCacheService _service;

    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();

    public PermissionCacheTests()
    {
        _cacheMock = new Mock<ICacheService>();
        _dbMock = new Mock<IRbacDbContext>();
        _loggerMock = new Mock<ILogger<PermissionCacheService>>();
        _service = new PermissionCacheService(_cacheMock.Object, _dbMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetUserPermissions_WhenCached_ReturnsCachedValue()
    {
        // Arrange
        var cached = new UserPermissions
        {
            Roles = new[] { "Admin" },
            Permissions = new[] { "users:read", "users:write" }
        };

        _cacheMock.Setup(c => c.GetAsync<UserPermissions>(
            $"perm:{TenantId}:{UserId}", It.IsAny<CancellationToken>()))
            .ReturnsAsync(cached);

        // Act
        var result = await _service.GetUserPermissionsAsync(TenantId, UserId);

        // Assert
        result.Roles.Should().BeEquivalentTo(cached.Roles);
        result.Permissions.Should().BeEquivalentTo(cached.Permissions);
        _dbMock.Verify(d => d.UserRoles, Times.Never);
    }

    [Fact]
    public async Task GetUserPermissions_WhenNotCached_QueriesDbAndCaches()
    {
        // Arrange
        _cacheMock.Setup(c => c.GetAsync<UserPermissions>(
            It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserPermissions?)null);

        var roleId = Guid.NewGuid();
        var permId = Guid.NewGuid();

        var role = new Role { Id = roleId, TenantId = TenantId, Name = "Staff" };
        var permission = new Permission { Id = permId, TenantId = TenantId, Name = "reports:read", Module = "reports", Action = "read" };
        role.RolePermissions = new List<RolePermission>
        {
            new() { RoleId = roleId, PermissionId = permId, Permission = permission }
        };

        var userRoles = new List<UserRole>
        {
            new() { UserId = UserId, RoleId = roleId, TenantId = TenantId, Role = role }
        }.AsQueryable();

        var mockUserRoles = CreateMockDbSet(userRoles);
        _dbMock.Setup(d => d.UserRoles).Returns(mockUserRoles.Object);

        _cacheMock.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<UserPermissions>(),
            It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.GetUserPermissionsAsync(TenantId, UserId);

        // Assert
        result.Should().NotBeNull();
        _cacheMock.Verify(c => c.SetAsync(
            $"perm:{TenantId}:{UserId}",
            It.IsAny<UserPermissions>(),
            TimeSpan.FromMinutes(30),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task InvalidateAsync_RemovesFromCache()
    {
        // Arrange
        _cacheMock.Setup(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.InvalidateAsync(TenantId, UserId);

        // Assert
        _cacheMock.Verify(c => c.RemoveAsync(
            $"perm:{TenantId}:{UserId}",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task InvalidateTenantAsync_RemovesAllTenantCacheEntries()
    {
        // Arrange
        _cacheMock.Setup(c => c.RemoveByPatternAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.InvalidateTenantAsync(TenantId);

        // Assert
        _cacheMock.Verify(c => c.RemoveByPatternAsync(
            $"perm:{TenantId}:*",
            It.IsAny<CancellationToken>()), Times.Once);
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
        var executeMethod = typeof(IQueryProvider).GetMethod(nameof(IQueryProvider.Execute))!.MakeGenericMethod(resultType);
        var result = executeMethod.Invoke(_inner, new object[] { expression })!;
        return (TResult)typeof(Task).GetMethod(nameof(Task.FromResult))!.MakeGenericMethod(resultType).Invoke(null, new[] { result })!;
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
