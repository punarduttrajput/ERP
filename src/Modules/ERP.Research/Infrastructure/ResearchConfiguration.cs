using ERP.Research.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Research.Infrastructure;

public class ResearchProjectConfiguration : IEntityTypeConfiguration<ResearchProject>
{
    public void Configure(EntityTypeBuilder<ResearchProject> builder)
    {
        builder.ToTable("research_projects");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasMaxLength(500).IsRequired();
        builder.Property(x => x.PrincipalInvestigatorName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.FundingAgency).HasMaxLength(300).IsRequired();
        builder.Property(x => x.FundingScheme).HasMaxLength(200);
        builder.Property(x => x.SanctionNumber).HasMaxLength(100);
        builder.Property(x => x.Abstract).HasMaxLength(5000);
        builder.Property(x => x.Domain).HasMaxLength(100);
        builder.Property(x => x.SanctionedAmount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.Status).HasConversion<int>();

        builder.HasMany(x => x.Members)
            .WithOne(x => x.Project)
            .HasForeignKey(x => x.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.TenantId, x.Status });
        builder.HasIndex(x => new { x.TenantId, x.PrincipalInvestigatorId });
    }
}

public class ProjectMemberConfiguration : IEntityTypeConfiguration<ProjectMember>
{
    public void Configure(EntityTypeBuilder<ProjectMember> builder)
    {
        builder.ToTable("project_members");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.MemberName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Role).HasConversion<int>();

        builder.HasIndex(x => new { x.TenantId, x.ProjectId, x.UserId }).IsUnique();
    }
}

public class PublicationConfiguration : IEntityTypeConfiguration<Publication>
{
    public void Configure(EntityTypeBuilder<Publication> builder)
    {
        builder.ToTable("publications");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FacultyName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.VenueName).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Isbn).HasMaxLength(20);
        builder.Property(x => x.IssueVolume).HasMaxLength(100);
        builder.Property(x => x.PageNumbers).HasMaxLength(50);
        builder.Property(x => x.Doi).HasMaxLength(200);
        builder.Property(x => x.ImpactFactor).HasColumnType("decimal(8,3)");
        builder.Property(x => x.PublicationType).HasConversion<int>();
        builder.Property(x => x.Index).HasConversion<int>();

        builder.HasIndex(x => new { x.TenantId, x.FacultyId });
        builder.HasIndex(x => new { x.TenantId, x.PublicationYear });
        builder.HasIndex(x => new { x.TenantId, x.Index });
    }
}

public class PatentConfiguration : IEntityTypeConfiguration<Patent>
{
    public void Configure(EntityTypeBuilder<Patent> builder)
    {
        builder.ToTable("patents");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Inventors).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.ApplicationNumber).HasMaxLength(100);
        builder.Property(x => x.GrantNumber).HasMaxLength(100);
        builder.Property(x => x.PatentOffice).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Status).HasConversion<int>();

        builder.HasIndex(x => new { x.TenantId, x.Status });
    }
}

public class GrantConfiguration : IEntityTypeConfiguration<Grant>
{
    public void Configure(EntityTypeBuilder<Grant> builder)
    {
        builder.ToTable("grants");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasMaxLength(500).IsRequired();
        builder.Property(x => x.FundingAgency).HasMaxLength(300).IsRequired();
        builder.Property(x => x.GrantNumber).HasMaxLength(100);
        builder.Property(x => x.SanctionedAmount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.DisbursedAmount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.UtilizedAmount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.Status).HasConversion<int>();

        builder.HasMany(x => x.Disbursements)
            .WithOne(x => x.Grant)
            .HasForeignKey(x => x.GrantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.TenantId, x.Status });
        builder.HasIndex(x => new { x.TenantId, x.PrincipalInvestigatorId });
    }
}

public class GrantDisbursementConfiguration : IEntityTypeConfiguration<GrantDisbursement>
{
    public void Configure(EntityTypeBuilder<GrantDisbursement> builder)
    {
        builder.ToTable("grant_disbursements");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Amount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.Reference).HasMaxLength(100);
        builder.Property(x => x.Notes).HasMaxLength(500);

        builder.HasIndex(x => new { x.TenantId, x.GrantId });
    }
}
