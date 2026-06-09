using ERP.SIS.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.SIS.Infrastructure;

public class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {
        builder.ToTable("students");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.StudentNumber).HasMaxLength(50).IsRequired();
        builder.Property(s => s.ProgramName).HasMaxLength(200).IsRequired();
        builder.Property(s => s.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(s => s.LastName).HasMaxLength(100).IsRequired();
        builder.Property(s => s.MiddleName).HasMaxLength(100);
        builder.Property(s => s.Email).HasMaxLength(320).IsRequired();
        builder.Property(s => s.MobileNumber).HasMaxLength(20).IsRequired();
        builder.Property(s => s.Gender).HasMaxLength(20).IsRequired();
        builder.Property(s => s.BloodGroup).HasMaxLength(10);
        builder.Property(s => s.PermanentAddress).HasMaxLength(500);
        builder.Property(s => s.CurrentAddress).HasMaxLength(500);
        builder.Property(s => s.Category).HasMaxLength(20).IsRequired();
        builder.Property(s => s.Semester).HasDefaultValue(1);

        builder.HasIndex(s => new { s.TenantId, s.StudentNumber }).IsUnique();
        builder.HasIndex(s => new { s.TenantId, s.ApplicationId }).IsUnique();

        builder.HasMany(s => s.Documents)
               .WithOne(d => d.Student)
               .HasForeignKey(d => d.StudentId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(s => s.FamilyDetails)
               .WithOne(f => f.Student)
               .HasForeignKey(f => f.StudentId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

public class StudentDocumentConfiguration : IEntityTypeConfiguration<StudentDocument>
{
    public void Configure(EntityTypeBuilder<StudentDocument> builder)
    {
        builder.ToTable("student_documents");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.DocumentType).HasMaxLength(100).IsRequired();
        builder.Property(d => d.OriginalFileName).HasMaxLength(255).IsRequired();
        builder.Property(d => d.BlobUrl).HasMaxLength(1000).IsRequired();
        builder.HasIndex(d => new { d.TenantId, d.StudentId });
    }
}

public class StudentFamilyConfiguration : IEntityTypeConfiguration<StudentFamily>
{
    public void Configure(EntityTypeBuilder<StudentFamily> builder)
    {
        builder.ToTable("student_family");
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Relation).HasMaxLength(50).IsRequired();
        builder.Property(f => f.Name).HasMaxLength(200).IsRequired();
        builder.Property(f => f.Occupation).HasMaxLength(200);
        builder.Property(f => f.MobileNumber).HasMaxLength(20);
        builder.Property(f => f.Email).HasMaxLength(320);
        builder.Property(f => f.AnnualIncome).HasColumnType("decimal(14,2)");
        builder.HasIndex(f => new { f.TenantId, f.StudentId });
    }
}
