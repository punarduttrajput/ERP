using ERP.Library.Application.Commands;
using ERP.Library.Domain;
using ERP.Library.Infrastructure;
using ERP.Shared.Application.Contracts;
using FluentAssertions;
using Xunit;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace ERP.UnitTests.Library;

// Minimal EF Core in-memory context that implements ILibraryDbContext for unit testing.
// A real AppDbContext cannot be used here because the test project does not reference ERP.Host.
internal class TestLibraryDbContext : DbContext, ILibraryDbContext
{
    public TestLibraryDbContext(DbContextOptions options) : base(options) { }

    public DbSet<Book> Books => Set<Book>();
    public DbSet<BookCopy> BookCopies => Set<BookCopy>();
    public DbSet<BookIssue> BookIssues => Set<BookIssue>();
    public DbSet<Fine> LibraryFines => Set<Fine>();
    public DbSet<LibraryPolicy> LibraryPolicies => Set<LibraryPolicy>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Book>().HasMany(x => x.Copies).WithOne(x => x.Book)
            .HasForeignKey(x => x.BookId);
        modelBuilder.Entity<BookIssue>().HasOne(x => x.Copy).WithMany()
            .HasForeignKey(x => x.CopyId);
        modelBuilder.Entity<Fine>().HasOne(x => x.Issue).WithMany()
            .HasForeignKey(x => x.IssueId);
    }
}

public class LibraryCirculationTests
{
    private static TestLibraryDbContext BuildDb() =>
        new(new DbContextOptionsBuilder()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid MemberId = Guid.NewGuid();

    private static (Book book, BookCopy copy) SeedAvailableCopy(ILibraryDbContext db)
    {
        var book = new Book
        {
            TenantId = TenantId,
            ISBN = "978-0-001",
            Title = "Test Book",
            Authors = "Author A",
            Language = "English",
            TotalCopies = 1,
            AvailableCopies = 1
        };

        var copy = new BookCopy
        {
            TenantId = TenantId,
            BookId = book.Id,
            Barcode = "BC001",
            Status = CopyStatus.Available,
            AcquisitionDate = DateOnly.FromDateTime(DateTime.UtcNow)
        };

        copy.Book = book;
        book.Copies.Add(copy);

        db.Books.Add(book);
        db.BookCopies.Add(copy);
        db.SaveChangesAsync(CancellationToken.None).GetAwaiter().GetResult();

        return (book, copy);
    }

    private static void SeedDefaultPolicy(ILibraryDbContext db, int gracePeriodDays = 0)
    {
        db.LibraryPolicies.Add(new LibraryPolicy
        {
            TenantId = TenantId,
            MemberType = MemberType.Student,
            MaxBooksAllowed = 3,
            LoanPeriodDays = 14,
            FinePerDay = 2m,
            MaxRenewals = 1,
            GracePeriodDays = gracePeriodDays
        });
        db.SaveChangesAsync(CancellationToken.None).GetAwaiter().GetResult();
    }

    [Fact]
    public async Task IssueBook_CopyNotAvailable_ReturnsFailure()
    {
        await using var db = BuildDb();
        SeedDefaultPolicy(db);
        var (_, copy) = SeedAvailableCopy(db);

        copy.Status = CopyStatus.Issued;
        await db.SaveChangesAsync();

        var feeMock = new Mock<IFeeService>();
        var result = await new IssueBookCommandHandler(db, feeMock.Object).Handle(
            new IssueBookCommand("BC001", MemberId, MemberType.Student, "Student A", DateTime.UtcNow, TenantId),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not available");
    }

    [Fact]
    public async Task IssueBook_ExceedsLimit_ReturnsFailure()
    {
        await using var db = BuildDb();
        SeedDefaultPolicy(db);

        for (var i = 0; i < 3; i++)
        {
            db.BookIssues.Add(new BookIssue
            {
                TenantId = TenantId,
                MemberId = MemberId,
                MemberType = MemberType.Student,
                CopyId = Guid.NewGuid(),
                BookId = Guid.NewGuid(),
                BookTitle = $"Book {i}",
                MemberName = "Student A",
                IssuedAt = DateTime.UtcNow,
                DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14)),
                Status = IssueStatus.Active
            });
        }
        await db.SaveChangesAsync();

        SeedAvailableCopy(db);
        var feeMock = new Mock<IFeeService>();
        var result = await new IssueBookCommandHandler(db, feeMock.Object).Handle(
            new IssueBookCommand("BC001", MemberId, MemberType.Student, "Student A", DateTime.UtcNow, TenantId),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("maximum concurrent issue limit");
    }

    [Fact]
    public async Task IssueBook_HasFees_ReturnsFailure()
    {
        await using var db = BuildDb();
        SeedDefaultPolicy(db);
        SeedAvailableCopy(db);

        var feeMock = new Mock<IFeeService>();
        feeMock.Setup(x => x.GetOutstandingBalanceAsync(MemberId, It.IsAny<CancellationToken>()))
               .ReturnsAsync(500m);

        var result = await new IssueBookCommandHandler(db, feeMock.Object).Handle(
            new IssueBookCommand("BC001", MemberId, MemberType.Student, "Student A", DateTime.UtcNow, TenantId),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("outstanding fee dues");
    }

    [Fact]
    public async Task IssueBook_Valid_SetsCopyToIssued_And_DecrementsAvailable()
    {
        await using var db = BuildDb();
        SeedDefaultPolicy(db);
        var (book, copy) = SeedAvailableCopy(db);

        var feeMock = new Mock<IFeeService>();
        feeMock.Setup(x => x.GetOutstandingBalanceAsync(MemberId, It.IsAny<CancellationToken>()))
               .ReturnsAsync(0m);

        var result = await new IssueBookCommandHandler(db, feeMock.Object).Handle(
            new IssueBookCommand("BC001", MemberId, MemberType.Student, "Student A", DateTime.UtcNow, TenantId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        copy.Status.Should().Be(CopyStatus.Issued);
        book.AvailableCopies.Should().Be(0);
    }

    [Fact]
    public async Task IssueBook_Valid_SetsDueDateCorrectly()
    {
        await using var db = BuildDb();
        SeedDefaultPolicy(db);
        SeedAvailableCopy(db);

        var feeMock = new Mock<IFeeService>();
        feeMock.Setup(x => x.GetOutstandingBalanceAsync(MemberId, It.IsAny<CancellationToken>()))
               .ReturnsAsync(0m);

        var issuedAt = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

        var result = await new IssueBookCommandHandler(db, feeMock.Object).Handle(
            new IssueBookCommand("BC001", MemberId, MemberType.Student, "Student A", issuedAt, TenantId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.DueDate.Should().Be(new DateOnly(2026, 6, 15));
    }

    [Fact]
    public async Task ReturnBook_OnTime_NoFine()
    {
        await using var db = BuildDb();
        SeedDefaultPolicy(db);
        var (book, copy) = SeedAvailableCopy(db);

        var issuedAt = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        db.BookIssues.Add(new BookIssue
        {
            TenantId = TenantId,
            CopyId = copy.Id,
            BookId = book.Id,
            BookTitle = book.Title,
            MemberId = MemberId,
            MemberType = MemberType.Student,
            MemberName = "Student A",
            IssuedAt = issuedAt,
            DueDate = new DateOnly(2026, 5, 15),
            Status = IssueStatus.Active
        });
        copy.Status = CopyStatus.Issued;
        book.AvailableCopies = 0;
        await db.SaveChangesAsync();

        var result = await new ReturnBookCommandHandler(db).Handle(
            new ReturnBookCommand("BC001", new DateTime(2026, 5, 10, 0, 0, 0, DateTimeKind.Utc), TenantId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Fine.Should().BeNull();
        copy.Status.Should().Be(CopyStatus.Available);
        book.AvailableCopies.Should().Be(1);
    }

    [Fact]
    public async Task ReturnBook_Overdue_CreatesFine()
    {
        await using var db = BuildDb();
        SeedDefaultPolicy(db); // FinePerDay = 2, GracePeriod = 0
        var (book, copy) = SeedAvailableCopy(db);

        db.BookIssues.Add(new BookIssue
        {
            TenantId = TenantId,
            CopyId = copy.Id,
            BookId = book.Id,
            BookTitle = book.Title,
            MemberId = MemberId,
            MemberType = MemberType.Student,
            MemberName = "Student A",
            IssuedAt = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
            DueDate = new DateOnly(2026, 5, 15),
            Status = IssueStatus.Active
        });
        copy.Status = CopyStatus.Issued;
        book.AvailableCopies = 0;
        await db.SaveChangesAsync();

        // Return 3 days late → 3 × ₹2 = ₹6
        var result = await new ReturnBookCommandHandler(db).Handle(
            new ReturnBookCommand("BC001", new DateTime(2026, 5, 18, 0, 0, 0, DateTimeKind.Utc), TenantId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Fine.Should().NotBeNull();
        result.Value.Fine!.DaysOverdue.Should().Be(3);
        result.Value.Fine.TotalFine.Should().Be(6m);
    }

    [Fact]
    public async Task ReturnBook_WithinGracePeriod_NoFine()
    {
        await using var db = BuildDb();
        SeedDefaultPolicy(db, gracePeriodDays: 2); // 1 day late, 2-day grace → no fine
        var (book, copy) = SeedAvailableCopy(db);

        db.BookIssues.Add(new BookIssue
        {
            TenantId = TenantId,
            CopyId = copy.Id,
            BookId = book.Id,
            BookTitle = book.Title,
            MemberId = MemberId,
            MemberType = MemberType.Student,
            MemberName = "Student A",
            IssuedAt = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
            DueDate = new DateOnly(2026, 5, 15),
            Status = IssueStatus.Active
        });
        copy.Status = CopyStatus.Issued;
        book.AvailableCopies = 0;
        await db.SaveChangesAsync();

        var result = await new ReturnBookCommandHandler(db).Handle(
            new ReturnBookCommand("BC001", new DateTime(2026, 5, 16, 0, 0, 0, DateTimeKind.Utc), TenantId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Fine.Should().BeNull();
    }
}
