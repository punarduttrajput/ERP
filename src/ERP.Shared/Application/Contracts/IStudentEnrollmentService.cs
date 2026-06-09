namespace ERP.Shared.Application.Contracts;

public interface IStudentEnrollmentService
{
    Task<bool> IsEnrolledAsync(Guid studentId, CancellationToken cancellationToken = default);
    Task<bool> IsStudentEnrolledAsync(Guid studentId, Guid courseId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Guid>> GetEnrolledCourseIdsAsync(Guid studentId, CancellationToken cancellationToken = default);
}
