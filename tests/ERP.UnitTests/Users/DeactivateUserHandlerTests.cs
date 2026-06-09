using ERP.Auth.Domain;
using ERP.Users.Application.Commands;
using ERP.Users.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using ERP.UnitTests.Auth;

namespace ERP.UnitTests.Users;

public class DeactivateUserHandlerTests
{
    private readonly Mock<IUsersDbContext> _dbMock = new();
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid UserId   = Guid.NewGuid();

    private readonly DeactivateUserHandler _handler;

    public DeactivateUserHandlerTests()
    {
        _dbMock.Setup(d => d.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _handler = new DeactivateUserHandler(_dbMock.Object);
    }

    [Fact]
    public async Task Handle_UserNotFound_ReturnsFailure()
    {
        SetupUserDbSet();

        var result = await _handler.Handle(new DeactivateUserCommand(UserId), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_ActiveUser_SetsIsActiveFalse()
    {
        var user = new User { Id = UserId, TenantId = TenantId, IsActive = true };
        SetupUserDbSet(user);

        var result = await _handler.Handle(new DeactivateUserCommand(UserId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        user.IsActive.Should().BeFalse();
        _dbMock.Verify(d => d.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_AlreadyInactiveUser_StillSucceeds()
    {
        var user = new User { Id = UserId, TenantId = TenantId, IsActive = false };
        SetupUserDbSet(user);

        var result = await _handler.Handle(new DeactivateUserCommand(UserId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        user.IsActive.Should().BeFalse();
    }

    private void SetupUserDbSet(params User[] users)
    {
        var set = CreateMockDbSet(users.AsQueryable());
        _dbMock.Setup(d => d.Users).Returns(set.Object);
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
