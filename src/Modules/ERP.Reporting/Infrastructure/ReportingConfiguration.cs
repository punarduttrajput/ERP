using ERP.Reporting.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Reporting.Infrastructure;

public class ReportDefinitionConfiguration : IEntityTypeConfiguration<ReportDefinition>
{
    public void Configure(EntityTypeBuilder<ReportDefinition> builder)
    {
        builder.ToTable("report_definitions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.Category).HasConversion<int>();
        builder.Property(x => x.SqlQuery).HasColumnType("longtext").IsRequired();
        builder.Property(x => x.DefaultColumns).HasMaxLength(5000);
        builder.Property(x => x.AvailableFilters).HasMaxLength(5000);
        builder.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
        builder.HasMany(x => x.Columns).WithOne(x => x.Report).HasForeignKey(x => x.ReportId);
        builder.HasMany(x => x.Filters).WithOne(x => x.Report).HasForeignKey(x => x.ReportId);
    }
}

public class ReportColumnConfiguration : IEntityTypeConfiguration<ReportColumn>
{
    public void Configure(EntityTypeBuilder<ReportColumn> builder)
    {
        builder.ToTable("report_columns");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ColumnName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.DataType).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Format).HasMaxLength(50);
        builder.HasIndex(x => new { x.TenantId, x.ReportId });
    }
}

public class ReportFilterConfiguration : IEntityTypeConfiguration<ReportFilter>
{
    public void Configure(EntityTypeBuilder<ReportFilter> builder)
    {
        builder.ToTable("report_filters");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FilterKey).HasMaxLength(100).IsRequired();
        builder.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.FilterType).HasMaxLength(50).IsRequired();
        builder.Property(x => x.DefaultValue).HasMaxLength(200);
        builder.Property(x => x.Options).HasMaxLength(2000);
        builder.HasIndex(x => new { x.TenantId, x.ReportId });
    }
}

public class ReportScheduleConfiguration : IEntityTypeConfiguration<ReportSchedule>
{
    public void Configure(EntityTypeBuilder<ReportSchedule> builder)
    {
        builder.ToTable("report_schedules");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Frequency).HasConversion<int>();
        builder.Property(x => x.ExportFormat).HasConversion<int>();
        builder.Property(x => x.Recipients).HasMaxLength(2000).IsRequired();
        builder.HasIndex(x => new { x.TenantId, x.ReportId });
    }
}

public class ReportExecutionConfiguration : IEntityTypeConfiguration<ReportExecution>
{
    public void Configure(EntityTypeBuilder<ReportExecution> builder)
    {
        builder.ToTable("report_executions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FiltersJson).HasMaxLength(2000);
        builder.Property(x => x.ExportFormat).HasConversion<int?>();
        builder.HasIndex(x => new { x.TenantId, x.ReportId });
        builder.HasIndex(x => new { x.TenantId, x.ExecutedAt });
    }
}
