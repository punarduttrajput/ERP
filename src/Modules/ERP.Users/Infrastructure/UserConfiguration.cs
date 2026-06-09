using ERP.Auth.Domain;
using ERP.Users.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Users.Infrastructure;

public class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        // Separate table — no conflict with 'users'
        builder.ToTable("user_profiles");

        // Shared primary key with users.id (1-to-1)
        builder.HasKey(p => p.Id);

        builder.Property(p => p.FirstName).HasMaxLength(100);
        builder.Property(p => p.LastName).HasMaxLength(100);
        builder.Property(p => p.PhoneNumber).HasMaxLength(50);
        builder.Property(p => p.AvatarUrl).HasMaxLength(500);
        builder.Property(p => p.Department).HasMaxLength(200);
        builder.Property(p => p.JobTitle).HasMaxLength(200);

        builder.HasQueryFilter(p => !p.IsDeleted);
    }
}

public interface IUsersDbContext
{
    /// <summary>Auth-owned identity record (email, password, IsActive, lockout).</summary>
    DbSet<User> Users { get; }

    /// <summary>Extended profile record (name, phone, avatar, dept, job title).</summary>
    DbSet<UserProfile> UserProfiles { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
