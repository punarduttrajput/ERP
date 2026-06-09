using ERP.Accreditation.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.NAAC.Application.Queries;

public record GetNaacMetricsQuery(int AcademicYear) : IRequest<Result<NaacMetricsDto>>;

public record NaacMetricsDto(
    decimal EnrollmentGrowthPercent,
    decimal StudentPassPercent,
    decimal AverageAttendancePercent,
    decimal FeeCollectionPercent,
    int TotalStudentsEnrolled,
    int TotalExamsPassed,
    int TotalFacultyCount);

public class GetNaacMetricsHandler : IRequestHandler<GetNaacMetricsQuery, Result<NaacMetricsDto>>
{
    private readonly IAccreditationDbContext _accreditationDb;

    public GetNaacMetricsHandler(IAccreditationDbContext accreditationDb) =>
        _accreditationDb = accreditationDb;

    public async Task<Result<NaacMetricsDto>> Handle(GetNaacMetricsQuery request, CancellationToken cancellationToken)
    {
        var summaries = await _accreditationDb.EvidenceSummaries
            .Where(s => s.AcademicYear == request.AcademicYear)
            .ToListAsync(cancellationToken);

        decimal Get(string module, string category, string key) =>
            summaries.FirstOrDefault(s => s.Module == module && s.Category == category && s.MetricKey == key)
                     ?.NumericValue ?? 0m;

        var totalEnrolled = (int)Get("SIS", "Enrollment", "total_enrolled");
        var totalPassed = (int)Get("Exams", "Results", "total_passed");

        var dto = new NaacMetricsDto(
            EnrollmentGrowthPercent: Get("SIS", "Enrollment", "growth_percent"),
            StudentPassPercent: Get("Exams", "Results", "pass_percent"),
            AverageAttendancePercent: Get("Attendance", "Summary", "average_percent"),
            FeeCollectionPercent: Get("Finance", "FeeCollection", "collection_percent"),
            TotalStudentsEnrolled: totalEnrolled,
            TotalExamsPassed: totalPassed,
            // HRMS faculty count will be populated in v4.0+; placeholder returns 0 until then
            TotalFacultyCount: (int)Get("HRMS", "Faculty", "total_count"));

        return Result<NaacMetricsDto>.Success(dto);
    }
}
