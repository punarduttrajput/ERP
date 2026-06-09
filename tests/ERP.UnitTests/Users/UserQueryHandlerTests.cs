using ERP.Auth.Domain;
using ERP.Users.Application.Queries;
using ERP.Users.Domain;
using ERP.Users.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using ERP.UnitTests.Auth;

namespace ERP.UnitTests.Users;

public class UserQueryHandlerTests
{
    private readonly Mock<IUsersDbContext> _dbMock = new();
    private static readonly Guid TenantId = Guid.NewGuid();

    // ── GetUserByIdHandler ────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_UserNotFound_ReturnsFailure()
    {
        SetupJoin(Array.Empty<User>(), Array.Empty<UserProfile>());
        var handler = new GetUserByIdHandler(_dbMock.Object);

        var result = await handler.Handle(new GetUserByIdQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task GetById_UserExists_ReturnsMappedDto()
    {
        var userId  = Guid.NewGuid();
        var user    = new User        { Id = userId, TenantId = TenantId, Email = "a@uni.com", IsActive = true };
        var profile = new UserProfile { Id = userId, TenantId = TenantId, FirstName = "Alice", LastName = "Smith" };
        SetupJoin(new[] { user }, new[] { profile });

        var handler = new GetUserByIdHandler(_dbMock.Object);
        var result  = await handler.Handle(new GetUserByIdQuery(userId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Email.Should().Be("a@uni.com");
        result.Value.FirstName.Should().Be("Alice");
    }

    // ── GetUsersHandler ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetUsers_NoFilter_ReturnsAllUsers()
    {
        var (users, profiles) = BuildUserSet(3);
        SetupJoin(users, profiles);

        var handler = new GetUsersHandler(_dbMock.Object);
        var result  = await handler.Handle(new GetUsersQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalCount.Should().Be(3);
        result.Value.Items.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetUsers_ActiveFilter_ReturnsOnlyActiveUsers()
    {
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var users = new[]
        {
            new User { Id = userId1, TenantId = TenantId, Email = "a@u.com", IsActive = true },
            new User { Id = userId2, TenantId = TenantId, Email = "b@u.com", IsActive = false }
        };
        var profiles = new[]
        {
            new UserProfile { Id = userId1, TenantId = TenantId },
            new UserProfile { Id = userId2, TenantId = TenantId }
        };
        SetupJoin(users, profiles);

        var handler = new GetUsersHandler(_dbMock.Object);
        var result  = await handler.Handle(new GetUsersQuery(IsActive: true), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().AllSatisfy(u => u.IsActive.Should().BeTrue());
    }

    [Fact]
    public async Task GetUsers_SearchByEmail_FiltersCorrectly()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var users = new[]
        {
            new User { Id = id1, TenantId = TenantId, Email = "alice@uni.com", IsActive = true },
            new User { Id = id2, TenantId = TenantId, Email = "bob@uni.com",   IsActive = true }
        };
        var profiles = new[]
        {
            new UserProfile { Id = id1, TenantId = TenantId, FirstName = "Alice" },
            new UserProfile { Id = id2, TenantId = TenantId, FirstName = "Bob" }
        };
        SetupJoin(users, profiles);

        var handler = new GetUsersHandler(_dbMock.Object);
        var result  = await handler.Handle(new GetUsersQuery(Search: "alice"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(1);
        result.Value.Items[0].Email.Should().Be("alice@uni.com");
    }

    [Fact]
    public async Task GetUsers_PageSize_IsClampedTo100()
    {
        var (users, profiles) = BuildUserSet(5);
        SetupJoin(users, profiles);

        var handler = new GetUsersHandler(_dbMock.Object);
        var result  = await handler.Handle(new GetUsersQuery(PageSize: 999), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.PageSize.Should().Be(100);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static (User[], UserProfile[]) BuildUserSet(int count)
    {
        var ids      = Enumerable.Range(0, count).Select(_ => Guid.NewGuid()).ToArray();
        var users    = ids.Select((id, i) => new User    { Id = id, TenantId = TenantId, Email = $"user{i}@uni.com", IsActive = true }).ToArray();
        var profiles = ids.Select((id, i) => new UserProfile { Id = id, TenantId = TenantId, FirstName = $"User{i}" }).ToArray();
        return (users, profiles);
    }

    private void SetupJoin(IEnumerable<User> users, IEnumerable<UserProfile> profiles)
    {
        _dbMock.Setup(d => d.Users).Returns(CreateMockDbSet(users.AsQueryable()).Object);
        _dbMock.Setup(d => d.UserProfiles).Returns(CreateMockDbSet(profiles.AsQueryable()).Object);
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
