using ERP.Timetable.Domain;
using Microsoft.EntityFrameworkCore;

namespace ERP.Timetable.Infrastructure;

public interface ITimetableDbContext
{
    DbSet<Room> Rooms { get; }
    DbSet<TimeSlot> TimeSlots { get; }
    DbSet<TimetableEntry> TimetableEntries { get; }
    DbSet<FacultyWorkload> FacultyWorkloads { get; }
    DbSet<FacultySubjectAssignment> FacultySubjectAssignments { get; }
    DbSet<SubstituteAssignment> SubstituteAssignments { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
