using ERP.MobileApi.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.MobileApi.Infrastructure;

public class DeviceRegistrationConfiguration : IEntityTypeConfiguration<DeviceRegistration>
{
    public void Configure(EntityTypeBuilder<DeviceRegistration> builder)
    {
        builder.ToTable("device_registrations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.DeviceToken).HasMaxLength(512).IsRequired();
        builder.Property(x => x.AppVersion).HasMaxLength(20);
        builder.Property(x => x.Platform).HasConversion<int>();
        // Unique constraint prevents duplicate token rows per user — upsert logic relies on this
        builder.HasIndex(x => new { x.TenantId, x.UserId, x.DeviceToken }).IsUnique();
    }
}

public class PushNotificationConfiguration : IEntityTypeConfiguration<PushNotification>
{
    public void Configure(EntityTypeBuilder<PushNotification> builder)
    {
        builder.ToTable("push_notifications");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Body).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.Data).HasMaxLength(2000);
        builder.Property(x => x.FailureReason).HasMaxLength(500);
        builder.Property(x => x.NotificationType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Status).HasConversion<int>();
        builder.Property(x => x.Platform).HasConversion<int>();
        builder.HasIndex(x => new { x.TenantId, x.RecipientUserId });
    }
}
