using ERP.Exams.Infrastructure;
using ERP.Shared.Application.Contracts;
using Microsoft.EntityFrameworkCore;

namespace ERP.Exams.Application.Services;

public class ExamResultEvidenceProvider : IEvidenceProvider
{
    private readonly IExamsDbContext _db;

    public ExamResultEvidenceProvider(IExamsDbContext db)
    {
        _db = db;
    }

    public string ModuleName => "Exams";

    public async Task<IReadOnlyList<EvidenceItem>> GetEvidenceAsync(
        Guid tenantId,
        int academicYear,
        CancellationToken cancellationToken = default)
    {
        var results = await _db.StudentResults
            .Where(r => r.TenantId == tenantId
                     && r.IsPublished
                     && !r.IsDeleted)
            .ToListAsync(cancellationToken);

        return results.Select(r => new EvidenceItem(
            Module: "Exams",
            Category: "ExamResult",
            Key: $"{r.StudentId}_{r.SubjectId}",
            Label: $"{r.SubjectName} — {r.GradeLetter}",
            NumericValue: r.MaxMarks > 0 ? r.TotalMarks / r.MaxMarks * 100 : null,
            TextValue: null,
            RecordedAt: r.PublishedAt ?? r.CreatedAt,
            Metadata: new Dictionary<string, string>
            {
                { "status", r.Status.ToString() },
                { "gpa", r.GPA?.ToString() ?? "" }
            }
        )).ToList();
    }
}
