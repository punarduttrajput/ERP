using ERP.Academics.Infrastructure;
using ERP.Shared.Application.Contracts;
using Microsoft.EntityFrameworkCore;

namespace ERP.Academics.Application.Services;

public class SubjectService : ISubjectService
{
    private readonly IAcademicsDbContext _db;

    public SubjectService(IAcademicsDbContext db)
    {
        _db = db;
    }

    public async Task<string?> GetSubjectNameAsync(Guid subjectId, CancellationToken cancellationToken = default)
    {
        return await _db.Subjects
            .Where(x => x.Id == subjectId && !x.IsDeleted)
            .Select(x => (string?)x.Name)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> GetSubjectIdsForProgramSemesterAsync(Guid programId, int semesterNumber, CancellationToken cancellationToken = default)
    {
        return await _db.CurriculumEntries
            .Where(x => x.ProgramId == programId && x.SemesterNumber == semesterNumber && !x.IsDeleted)
            .Select(x => x.SubjectId)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> SubjectExistsAsync(Guid subjectId, CancellationToken cancellationToken = default)
    {
        return await _db.Subjects
            .AnyAsync(x => x.Id == subjectId && !x.IsDeleted, cancellationToken);
    }
}
