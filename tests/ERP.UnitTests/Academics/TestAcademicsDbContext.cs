using ERP.Academics.Domain;
using ERP.Academics.Infrastructure;
using ERP.Shared.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace ERP.UnitTests.Academics;

public class TestAcademicsDbContext : DbContext, IAcademicsDbContext
{
    private readonly ICurrentTenant _currentTenant;

    public TestAcademicsDbContext(DbContextOptions<TestAcademicsDbContext> options, ICurrentTenant currentTenant)
        : base(options)
    {
        _currentTenant = currentTenant;
    }

    public DbSet<Department> Departments => Set<Department>();
    public DbSet<AcademicProgram> AcademicPrograms => Set<AcademicProgram>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<AcademicYear> AcademicYears => Set<AcademicYear>();
    public DbSet<Semester> Semesters => Set<Semester>();
    public DbSet<Batch> Batches => Set<Batch>();
    public DbSet<CurriculumEntry> CurriculumEntries => Set<CurriculumEntry>();
    public DbSet<CourseOutcome> CourseOutcomes => Set<CourseOutcome>();
    public DbSet<ProgramOutcome> ProgramOutcomes => Set<ProgramOutcome>();
    public DbSet<ProgramSpecificOutcome> ProgramSpecificOutcomes => Set<ProgramSpecificOutcome>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Tenant filter that reads from the injected ICurrentTenant at query time
        var tenantId = _currentTenant.TenantId ?? Guid.Empty;

        modelBuilder.Entity<Department>().HasQueryFilter(x => x.TenantId == tenantId);
        modelBuilder.Entity<AcademicProgram>().HasQueryFilter(x => x.TenantId == tenantId);
        modelBuilder.Entity<Course>().HasQueryFilter(x => x.TenantId == tenantId);
        modelBuilder.Entity<Subject>().HasQueryFilter(x => x.TenantId == tenantId);
        modelBuilder.Entity<AcademicYear>().HasQueryFilter(x => x.TenantId == tenantId);
        modelBuilder.Entity<Semester>().HasQueryFilter(x => x.TenantId == tenantId);
        modelBuilder.Entity<Batch>().HasQueryFilter(x => x.TenantId == tenantId);
        modelBuilder.Entity<CurriculumEntry>().HasQueryFilter(x => x.TenantId == tenantId);
        modelBuilder.Entity<CourseOutcome>().HasQueryFilter(x => x.TenantId == tenantId);
        modelBuilder.Entity<ProgramOutcome>().HasQueryFilter(x => x.TenantId == tenantId);
        modelBuilder.Entity<ProgramSpecificOutcome>().HasQueryFilter(x => x.TenantId == tenantId);
    }
}
