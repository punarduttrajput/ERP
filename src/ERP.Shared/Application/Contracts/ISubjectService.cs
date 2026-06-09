namespace ERP.Shared.Application.Contracts;

public interface ISubjectService
{
    Task<string?> GetSubjectNameAsync(Guid subjectId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Guid>> GetSubjectIdsForProgramSemesterAsync(Guid programId, int semesterNumber, CancellationToken cancellationToken = default);
    Task<bool> SubjectExistsAsync(Guid subjectId, CancellationToken cancellationToken = default);
}
