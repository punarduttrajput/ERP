using ERP.Shared.Application.Contracts;
using ERP.SIS.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ERP.SIS.Application.Services;

public class StudentEnrollmentEvidenceProvider : IEvidenceProvider
{
    private readonly ISisDbContext _db;

    public StudentEnrollmentEvidenceProvider(ISisDbContext db)
    {
        _db = db;
    }

    public string ModuleName => "SIS";

    public async Task<IReadOnlyList<EvidenceItem>> GetEvidenceAsync(
        Guid tenantId,
        int academicYear,
        CancellationToken cancellationToken = default)
    {
        var students = await _db.Students
            .Where(s => s.TenantId == tenantId
                     && s.AcademicYear == academicYear
                     && s.IsActive
                     && !s.IsDeleted)
            .ToListAsync(cancellationToken);

        return students.Select(s => new EvidenceItem(
            Module: "SIS",
            Category: "StudentEnrollment",
            Key: s.Id.ToString(),
            Label: $"{s.FirstName} {s.LastName} — {s.ProgramName}",
            NumericValue: null,
            TextValue: s.ProgramName,
            RecordedAt: s.EnrolledAt,
            Metadata: new Dictionary<string, string>
            {
                { "category", s.Category },
                { "semester", s.Semester.ToString() }
            }
        )).ToList();
    }
}
