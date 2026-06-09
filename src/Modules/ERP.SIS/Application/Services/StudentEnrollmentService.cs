using ERP.Shared.Application.Contracts;
using ERP.SIS.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ERP.SIS.Application.Services;

public class StudentEnrollmentService : IStudentEnrollmentService
{
    private readonly ISisDbContext _db;

    public StudentEnrollmentService(ISisDbContext db)
    {
        _db = db;
    }

    public Task<bool> IsEnrolledAsync(Guid studentId, CancellationToken cancellationToken = default)
        => _db.Students.AnyAsync(s => s.Id == studentId && s.IsActive, cancellationToken);

    public async Task<bool> IsStudentEnrolledAsync(Guid studentId, Guid courseId, CancellationToken cancellationToken = default)
        => await IsEnrolledAsync(studentId, cancellationToken);

    // Course-level enrollment is implemented in v2.3; returns empty list for now.
    public Task<IReadOnlyList<Guid>> GetEnrolledCourseIdsAsync(Guid studentId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Guid>>(Array.Empty<Guid>());
}
