using ERP.Academics.Domain;
using Microsoft.EntityFrameworkCore;

namespace ERP.Academics.Infrastructure;

public interface IAcademicsDbContext
{
    DbSet<Department> Departments { get; }
    DbSet<AcademicProgram> AcademicPrograms { get; }
    DbSet<Course> Courses { get; }
    DbSet<Subject> Subjects { get; }
    DbSet<AcademicYear> AcademicYears { get; }
    DbSet<Semester> Semesters { get; }
    DbSet<Batch> Batches { get; }
    DbSet<CurriculumEntry> CurriculumEntries { get; }
    DbSet<CourseOutcome> CourseOutcomes { get; }
    DbSet<ProgramOutcome> ProgramOutcomes { get; }
    DbSet<ProgramSpecificOutcome> ProgramSpecificOutcomes { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
