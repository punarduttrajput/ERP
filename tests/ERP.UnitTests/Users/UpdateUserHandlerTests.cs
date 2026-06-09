using ERP.Auth.Domain;
using ERP.Users.Application.Commands;
using ERP.Users.Domain;
using ERP.Users.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using ERP.UnitTests.Auth;

namespace ERP.UnitTests.Users;

public class UpdateUserHandlerTests
{
    private readonly Mock<IUsersDbContext> _dbMock = new();
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid UserId   = Guid.NewGuid();

    private readonly UpdateUserHandler _handler;

    public UpdateUserHandlerTests()
    {
        _dbMock.Setup(d => d.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _handler = new UpdateUserHandler(_dbMock.Object);
    }

    [Fact]
    public async Task Handle_UserNotFound_ReturnsFailure()
    {
        SetupProfileDbSet();
        SetupUserDbSet();

        var result = await _handler.Handle(new UpdateUserCommand(UserId, "Jane", null, null, null, null, null), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_ValidUpdate_UpdatesProfileFields()
    {
        var profile = new UserProfile { Id = UserId, TenantId = TenantId, FirstName = "Old", LastName = "Name" };
        var user    = new User        { Id = UserId, TenantId = TenantId, FirstName = "Old", LastName = "Name" };
        SetupProfileDbSet(profile);
        SetupUserDbSet(user);

        var result = await _handler.Handle(
            new UpdateUserCommand(UserId, "Jane", "Smith", "555-9999", "HR", "Manager", null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        profile.FirstName.Should().Be("Jane");
        profile.LastName.Should().Be("Smith");
        profile.PhoneNumber.Should().Be("555-9999");
        profile.Department.Should().Be("HR");
        profile.JobTitle.Should().Be("Manager");
        user.FirstName.Should().Be("Jane");
        user.LastName.Should().Be("Smith");
    }

    [Fact]
    public async Task Handle_NullFields_DoesNotOverwrite()
    {
        var profile = new UserProfile { Id = UserId, TenantId = TenantId, FirstName = "Keep", Department = "Engineering" };
        var user    = new User        { Id = UserId, TenantId = TenantId };
        SetupProfileDbSet(profile);
        SetupUserDbSet(user);

        await _handler.Handle(new UpdateUserCommand(UserId, null, null, null, null, null, null), CancellationToken.None);

        profile.FirstName.Should().Be("Keep");
        profile.Department.Should().Be("Engineering");
    }

    private void SetupProfileDbSet(params UserProfile[] profiles)
    {
        var set = CreateMockDbSet(profiles.AsQueryable());
        _dbMock.Setup(d => d.UserProfiles).Returns(set.Object);
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
