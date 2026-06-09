using ERP.Library.Domain;
using Microsoft.EntityFrameworkCore;

namespace ERP.Library.Infrastructure;

public interface ILibraryDbContext
{
    DbSet<Book> Books { get; }
    DbSet<BookCopy> BookCopies { get; }
    DbSet<BookIssue> BookIssues { get; }
    DbSet<Fine> LibraryFines { get; }
    DbSet<LibraryPolicy> LibraryPolicies { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
