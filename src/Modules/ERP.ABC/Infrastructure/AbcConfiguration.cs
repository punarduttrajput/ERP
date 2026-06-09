using ERP.ABC.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.ABC.Infrastructure;

public class StudentAbcProfileConfiguration : IEntityTypeConfiguration<StudentAbcProfile>
{
    public void Configure(EntityTypeBuilder<StudentAbcProfile> builder)
    {
        builder.ToTable("student_abc_profiles");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.AbcId).HasMaxLength(20).IsRequired();
        builder.Property(x => x.RegistryStudentName).HasMaxLength(200);
        builder.Property(x => x.ActivePathwayType).HasConversion<int?>().IsRequired(false);

        builder.HasIndex(x => new { x.TenantId, x.StudentId }).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.AbcId }).IsUnique();
    }
}

public class CreditTransferConfiguration : IEntityTypeConfiguration<CreditTransfer>
{
    public void Configure(EntityTypeBuilder<CreditTransfer> builder)
    {
        builder.ToTable("credit_transfers");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.AbcId).HasMaxLength(20).IsRequired();
        builder.Property(x => x.SourceInstitution).HasMaxLength(300).IsRequired();
        builder.Property(x => x.DestinationInstitution).HasMaxLength(300);
        builder.Property(x => x.SubjectCode).HasMaxLength(50).IsRequired();
        builder.Property(x => x.SubjectName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.RejectionReason).HasMaxLength(500);
        builder.Property(x => x.AbcRegistryReference).HasMaxLength(100);
        builder.Property(x => x.Direction).HasConversion<int>();
        builder.Property(x => x.Status).HasConversion<int>();

        builder.HasIndex(x => new { x.TenantId, x.StudentId });
        builder.HasIndex(x => new { x.TenantId, x.Status });
    }
}

public class AcademicPathwayConfiguration : IEntityTypeConfiguration<AcademicPathway>
{
    public void Configure(EntityTypeBuilder<AcademicPathway> builder)
    {
        builder.ToTable("academic_pathways");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.PathwayType).HasConversion<int>();
        builder.Property(x => x.Status).HasMaxLength(50).IsRequired();

        builder.HasIndex(x => new { x.TenantId, x.StudentId });
    }
}
