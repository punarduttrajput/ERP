using ERP.Auth.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Auth.Infrastructure;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Email).HasMaxLength(320).IsRequired();
        builder.HasIndex(u => new { u.TenantId, u.Email }).IsUnique();
        builder.Property(u => u.PasswordHash).HasMaxLength(100).IsRequired();
        builder.Property(u => u.FirstName).HasMaxLength(100);
        builder.Property(u => u.LastName).HasMaxLength(100);

        builder.HasMany(u => u.RefreshTokens)
            .WithOne(r => r.User)
            .HasForeignKey(r => r.UserId);

        builder.Property(u => u.MobileNumber).HasMaxLength(20);
        builder.HasIndex(u => new { u.TenantId, u.MobileNumber }).IsUnique().HasFilter("MobileNumber IS NOT NULL");

        builder.Property(u => u.MfaSecret).HasMaxLength(100);
        builder.Property(u => u.MfaRecoveryCodes).HasColumnType("text");

        builder.HasQueryFilter(u => !u.IsDeleted);
    }
}

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Token).HasMaxLength(200).IsRequired();
        builder.HasIndex(r => r.Token).IsUnique();
        builder.Property(r => r.FamilyId).HasMaxLength(50).IsRequired();
        builder.HasIndex(r => r.FamilyId);
        builder.Property(r => r.ReplacedByToken).HasMaxLength(200);
        builder.Property(r => r.RevokedReason).HasMaxLength(200);
        builder.Property(r => r.CreatedByIp).HasMaxLength(50);

        builder.HasQueryFilter(r => !r.IsDeleted);
    }
}
