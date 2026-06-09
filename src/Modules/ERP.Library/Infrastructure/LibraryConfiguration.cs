using ERP.Library.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Library.Infrastructure;

public class BookConfiguration : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> builder)
    {
        builder.ToTable("library_books");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnType("char(36)");
        builder.Property(x => x.TenantId).HasColumnType("char(36)");
        builder.Property(x => x.CreatedBy).HasColumnType("char(36)");
        builder.Property(x => x.ISBN).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Authors).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Publisher).HasMaxLength(200);
        builder.Property(x => x.Edition).HasMaxLength(50);
        builder.Property(x => x.Category).HasMaxLength(100);
        builder.Property(x => x.Language).HasMaxLength(20).HasDefaultValue("English");
        builder.Property(x => x.ShelfLocation).HasMaxLength(100);
        builder.Property(x => x.CoverImageUrl).HasMaxLength(1000);
        builder.Property(x => x.TotalCopies).HasDefaultValue(0);
        builder.Property(x => x.AvailableCopies).HasDefaultValue(0);

        builder.HasIndex(x => new { x.TenantId, x.ISBN }).IsUnique()
            .HasDatabaseName("IX_library_books_TenantId_ISBN");

        builder.HasMany(x => x.Copies)
            .WithOne(x => x.Book)
            .HasForeignKey(x => x.BookId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class BookCopyConfiguration : IEntityTypeConfiguration<BookCopy>
{
    public void Configure(EntityTypeBuilder<BookCopy> builder)
    {
        builder.ToTable("book_copies");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnType("char(36)");
        builder.Property(x => x.TenantId).HasColumnType("char(36)");
        builder.Property(x => x.CreatedBy).HasColumnType("char(36)");
        builder.Property(x => x.BookId).HasColumnType("char(36)");
        builder.Property(x => x.Barcode).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Status).HasConversion<int>();
        builder.Property(x => x.Price).HasColumnType("decimal(18,2)");

        builder.HasIndex(x => new { x.TenantId, x.Barcode }).IsUnique()
            .HasDatabaseName("IX_book_copies_TenantId_Barcode");
    }
}

public class BookIssueConfiguration : IEntityTypeConfiguration<BookIssue>
{
    public void Configure(EntityTypeBuilder<BookIssue> builder)
    {
        builder.ToTable("book_issues");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnType("char(36)");
        builder.Property(x => x.TenantId).HasColumnType("char(36)");
        builder.Property(x => x.CreatedBy).HasColumnType("char(36)");
        builder.Property(x => x.CopyId).HasColumnType("char(36)");
        builder.Property(x => x.BookId).HasColumnType("char(36)");
        builder.Property(x => x.MemberId).HasColumnType("char(36)");
        builder.Property(x => x.BookTitle).HasMaxLength(500).IsRequired();
        builder.Property(x => x.MemberName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.MemberType).HasConversion<int>();
        builder.Property(x => x.Status).HasConversion<int>();

        builder.HasIndex(x => new { x.TenantId, x.MemberId })
            .HasDatabaseName("IX_book_issues_TenantId_MemberId");

        builder.HasOne(x => x.Copy)
            .WithMany()
            .HasForeignKey(x => x.CopyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class FineConfiguration : IEntityTypeConfiguration<Fine>
{
    public void Configure(EntityTypeBuilder<Fine> builder)
    {
        builder.ToTable("library_fines");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnType("char(36)");
        builder.Property(x => x.TenantId).HasColumnType("char(36)");
        builder.Property(x => x.CreatedBy).HasColumnType("char(36)");
        builder.Property(x => x.IssueId).HasColumnType("char(36)");
        builder.Property(x => x.MemberId).HasColumnType("char(36)");
        builder.Property(x => x.WaivedBy).HasColumnType("char(36)");
        builder.Property(x => x.FinePerDay).HasColumnType("decimal(18,2)");
        builder.Property(x => x.TotalFine).HasColumnType("decimal(18,2)");
        builder.Property(x => x.Status).HasConversion<int>();
        builder.Property(x => x.WaivedReason).HasMaxLength(500);

        builder.HasIndex(x => new { x.TenantId, x.MemberId })
            .HasDatabaseName("IX_library_fines_TenantId_MemberId");

        builder.HasOne(x => x.Issue)
            .WithMany()
            .HasForeignKey(x => x.IssueId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class LibraryPolicyConfiguration : IEntityTypeConfiguration<LibraryPolicy>
{
    public void Configure(EntityTypeBuilder<LibraryPolicy> builder)
    {
        builder.ToTable("library_policies");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnType("char(36)");
        builder.Property(x => x.TenantId).HasColumnType("char(36)");
        builder.Property(x => x.CreatedBy).HasColumnType("char(36)");
        builder.Property(x => x.MemberType).HasConversion<int>();
        builder.Property(x => x.FinePerDay).HasColumnType("decimal(18,2)");

        builder.HasIndex(x => new { x.TenantId, x.MemberType }).IsUnique()
            .HasDatabaseName("IX_library_policies_TenantId_MemberType");
    }
}
