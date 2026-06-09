using ERP.Admissions.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Admissions.Infrastructure;

public class AdmissionApplicationConfiguration : IEntityTypeConfiguration<AdmissionApplication>
{
    public void Configure(EntityTypeBuilder<AdmissionApplication> builder)
    {
        builder.ToTable("admission_applications");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.ApplicantName).HasMaxLength(200).IsRequired();
        builder.Property(a => a.ApplicantEmail).HasMaxLength(320).IsRequired();
        builder.Property(a => a.ApplicantMobile).HasMaxLength(20).IsRequired();
        builder.Property(a => a.ProgramName).HasMaxLength(200).IsRequired();
        builder.Property(a => a.Category).HasMaxLength(20).IsRequired();
        builder.Property(a => a.State).HasConversion<int>();
        builder.Property(a => a.RejectionReason).HasMaxLength(500);
        builder.Property(a => a.MeritScore).HasPrecision(8, 4);
        builder.HasMany(a => a.Documents).WithOne(d => d.Application).HasForeignKey(d => d.ApplicationId);
        builder.HasMany(a => a.AuditEntries).WithOne(e => e.Application).HasForeignKey(e => e.ApplicationId);
        builder.HasQueryFilter(a => !a.IsDeleted);
    }
}

public class ApplicationDocumentConfiguration : IEntityTypeConfiguration<ApplicationDocument>
{
    public void Configure(EntityTypeBuilder<ApplicationDocument> builder)
    {
        builder.ToTable("admission_documents");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.DocumentType).HasMaxLength(100).IsRequired();
        builder.Property(d => d.BlobUrl).HasMaxLength(1000).IsRequired();
        builder.Property(d => d.FileName).HasMaxLength(255).IsRequired();
        builder.Property(d => d.VerificationRemark).HasMaxLength(500);
        builder.HasQueryFilter(d => !d.IsDeleted);
    }
}

public class WorkflowAuditEntryConfiguration : IEntityTypeConfiguration<WorkflowAuditEntry>
{
    public void Configure(EntityTypeBuilder<WorkflowAuditEntry> builder)
    {
        builder.ToTable("admission_workflow_audit");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.FromState).HasConversion<int>();
        builder.Property(e => e.ToState).HasConversion<int>();
        builder.Property(e => e.Reason).HasMaxLength(500);
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}

public class WorkflowDefinitionConfiguration : IEntityTypeConfiguration<WorkflowDefinition>
{
    public void Configure(EntityTypeBuilder<WorkflowDefinition> builder)
    {
        builder.ToTable("admission_workflow_definitions");
        builder.HasKey(w => w.Id);
        builder.Property(w => w.Name).HasMaxLength(200).IsRequired();
        builder.HasQueryFilter(w => !w.IsDeleted);
    }
}

public class SeatMatrixConfiguration : IEntityTypeConfiguration<SeatMatrix>
{
    public void Configure(EntityTypeBuilder<SeatMatrix> builder)
    {
        builder.ToTable("admission_seat_matrix");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.ProgramName).HasMaxLength(200).IsRequired();
        builder.Property(s => s.Category).HasMaxLength(20).IsRequired();
        builder.HasIndex(s => new { s.TenantId, s.ProgramId, s.AcademicYear, s.Category }).IsUnique();
        builder.HasQueryFilter(s => !s.IsDeleted);
    }
}
