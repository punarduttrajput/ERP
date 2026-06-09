using ERP.Attendance.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Attendance.Infrastructure;

public class AttendanceSessionConfiguration : IEntityTypeConfiguration<AttendanceSession>
{
    public void Configure(EntityTypeBuilder<AttendanceSession> builder)
    {
        builder.ToTable("attendance_sessions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.SubjectId).HasColumnType("char(36)").IsRequired();
        builder.Property(x => x.BatchId).HasColumnType("char(36)").IsRequired();
        builder.Property(x => x.SemesterId).HasColumnType("char(36)").IsRequired();
        builder.Property(x => x.FacultyUserId).HasColumnType("char(36)").IsRequired();
        builder.Property(x => x.SessionDate).HasColumnType("date").IsRequired();
        builder.Property(x => x.PeriodNumber).IsRequired();
        builder.Property(x => x.IsLocked).HasDefaultValue(false);
        builder.Property(x => x.QrToken).HasMaxLength(100);
        builder.Property(x => x.QrExpiresAt).HasColumnType("datetime(6)");
        builder.Property(x => x.TenantId).HasColumnType("char(36)").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnType("datetime(6)");
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime(6)");
        builder.Property(x => x.CreatedBy).HasColumnType("char(36)");

        builder.HasIndex(x => new { x.TenantId, x.SubjectId, x.BatchId, x.SessionDate, x.PeriodNumber })
            .IsUnique()
            .HasDatabaseName("IX_attendance_sessions_TenantId_SubjectId_BatchId_SessionDate_PeriodNumber");

        builder.HasMany(x => x.Records)
            .WithOne(r => r.Session)
            .HasForeignKey(r => r.SessionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class AttendanceRecordConfiguration : IEntityTypeConfiguration<AttendanceRecord>
{
    public void Configure(EntityTypeBuilder<AttendanceRecord> builder)
    {
        builder.ToTable("attendance_records");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.SessionId).HasColumnType("char(36)").IsRequired();
        builder.Property(x => x.StudentId).HasColumnType("char(36)").IsRequired();
        builder.Property(x => x.Status).HasConversion<int>().IsRequired();
        builder.Property(x => x.MarkedAt).HasColumnType("datetime(6)").IsRequired();
        builder.Property(x => x.MarkedBy).HasMaxLength(50).IsRequired();
        builder.Property(x => x.TenantId).HasColumnType("char(36)").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnType("datetime(6)");
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime(6)");
        builder.Property(x => x.CreatedBy).HasColumnType("char(36)");

        builder.HasIndex(x => new { x.TenantId, x.SessionId, x.StudentId })
            .IsUnique()
            .HasDatabaseName("IX_attendance_records_TenantId_SessionId_StudentId");
    }
}

public class RegularizationRequestConfiguration : IEntityTypeConfiguration<RegularizationRequest>
{
    public void Configure(EntityTypeBuilder<RegularizationRequest> builder)
    {
        builder.ToTable("regularization_requests");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.SessionId).HasColumnType("char(36)").IsRequired();
        builder.Property(x => x.StudentId).HasColumnType("char(36)").IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.Status).HasConversion<int>().IsRequired();
        builder.Property(x => x.ReviewedBy).HasColumnType("char(36)");
        builder.Property(x => x.ReviewedAt).HasColumnType("datetime(6)");
        builder.Property(x => x.ReviewRemark).HasMaxLength(500);
        builder.Property(x => x.TenantId).HasColumnType("char(36)").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnType("datetime(6)");
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime(6)");
        builder.Property(x => x.CreatedBy).HasColumnType("char(36)");
    }
}

public class BiometricLogConfiguration : IEntityTypeConfiguration<BiometricLog>
{
    public void Configure(EntityTypeBuilder<BiometricLog> builder)
    {
        builder.ToTable("biometric_logs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.DeviceId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.BiometricId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.StudentId).HasColumnType("char(36)");
        builder.Property(x => x.LoggedAt).HasColumnType("datetime(6)").IsRequired();
        builder.Property(x => x.IsProcessed).HasDefaultValue(false);
        builder.Property(x => x.TenantId).HasColumnType("char(36)").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnType("datetime(6)");
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime(6)");
        builder.Property(x => x.CreatedBy).HasColumnType("char(36)");
    }
}
