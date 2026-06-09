using ERP.Exams.Domain;
using Microsoft.EntityFrameworkCore;

namespace ERP.Exams.Infrastructure;

public interface IExamsDbContext
{
    DbSet<ExamSchedule> ExamSchedules { get; }
    DbSet<SeatAllocation> SeatAllocations { get; }
    DbSet<GradingScheme> GradingSchemes { get; }
    DbSet<GradeRule> GradeRules { get; }
    DbSet<InternalMark> InternalMarks { get; }
    DbSet<ExternalMark> ExternalMarks { get; }
    DbSet<StudentResult> StudentResults { get; }
    DbSet<ArrearRegistration> ArrearRegistrations { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
