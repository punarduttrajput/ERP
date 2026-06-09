using ERP.OBE.Domain;
using ERP.OBE.Infrastructure;
using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.OBE.Application.Queries;

public record GetNbaReportQuery(Guid TenantId, Guid ProgramId, int AcademicYear) : IRequest<Result<byte[]>>;

public class GetNbaReportHandler : IRequestHandler<GetNbaReportQuery, Result<byte[]>>
{
    private readonly IObeDbContext _db;
    private readonly IPdfService _pdfService;

    public GetNbaReportHandler(IObeDbContext db, IPdfService pdfService)
    {
        _db = db;
        _pdfService = pdfService;
    }

    public async Task<Result<byte[]>> Handle(GetNbaReportQuery request, CancellationToken cancellationToken)
    {
        var coPoMappings = await _db.CoPoMappings
            .Where(x => x.TenantId == request.TenantId
                     && x.ProgramId == request.ProgramId
                     && !x.IsDeleted)
            .ToListAsync(cancellationToken);

        var subjectIds = coPoMappings.Select(m => m.SubjectId).Distinct().ToList();

        var directAttainments = await _db.DirectAttainments
            .Where(x => x.TenantId == request.TenantId
                     && subjectIds.Contains(x.SubjectId)
                     && x.AcademicYear == request.AcademicYear
                     && !x.IsDeleted)
            .ToListAsync(cancellationToken);

        var actionPlans = await _db.ActionPlans
            .Where(x => x.TenantId == request.TenantId
                     && subjectIds.Contains(x.SubjectId)
                     && !x.IsDeleted)
            .ToListAsync(cancellationToken);

        var gaps = await _db.AttainmentGaps
            .Where(x => x.TenantId == request.TenantId
                     && subjectIds.Contains(x.SubjectId)
                     && x.AcademicYear == request.AcademicYear
                     && !x.IsDeleted)
            .ToListAsync(cancellationToken);

        var poAttainments = ComputePoAttainments(coPoMappings, directAttainments);

        var html = BuildHtml(request.ProgramId, request.AcademicYear, coPoMappings, directAttainments, poAttainments, gaps, actionPlans);
        var bytes = await _pdfService.GeneratePdfAsync(html, cancellationToken);
        return Result<byte[]>.Success(bytes);
    }

    // PO_attainment[PO_n] = Sum(CO_attainment[CO_i] * correlation[CO_i,PO_n]) / Sum(correlation[CO_i,PO_n])
    // Only non-zero correlations contribute to the weighted average
    public static Dictionary<string, decimal> ComputePoAttainments(
        IEnumerable<CoPoMapping> mappings,
        IEnumerable<DirectAttainment> attainments)
    {
        var attainmentByCoKey = attainments
            .GroupBy(a => (a.SubjectId, a.CourseOutcomeCode))
            .ToDictionary(g => g.Key, g => g.Average(a => (double)a.AttainmentPercent));

        var poGroups = mappings
            .Where(m => m.CorrelationLevel > 0)
            .GroupBy(m => m.ProgramOutcomeCode);

        var result = new Dictionary<string, decimal>();

        foreach (var poGroup in poGroups)
        {
            double weightedSum = 0;
            double correlationSum = 0;

            foreach (var mapping in poGroup)
            {
                var key = (mapping.SubjectId, mapping.CourseOutcomeCode);
                if (!attainmentByCoKey.TryGetValue(key, out var coAtt))
                    continue;

                weightedSum += coAtt * mapping.CorrelationLevel;
                correlationSum += mapping.CorrelationLevel;
            }

            if (correlationSum > 0)
                result[poGroup.Key] = Math.Round((decimal)(weightedSum / correlationSum), 2);
        }

        return result;
    }

    private static string BuildHtml(
        Guid programId,
        int academicYear,
        List<CoPoMapping> coPoMappings,
        List<DirectAttainment> directAttainments,
        Dictionary<string, decimal> poAttainments,
        List<AttainmentGap> gaps,
        List<ActionPlan> actionPlans)
    {
        var coPoRows = coPoMappings
            .OrderBy(m => m.SubjectId)
            .ThenBy(m => m.CourseOutcomeCode)
            .ThenBy(m => m.ProgramOutcomeCode);

        var daRows = directAttainments
            .OrderBy(d => d.SubjectId)
            .ThenBy(d => d.CourseOutcomeCode);

        var html = $@"<!DOCTYPE html>
<html>
<head>
<meta charset='utf-8'/>
<style>
  body {{ font-family: Arial, sans-serif; font-size: 12px; margin: 20px; }}
  h1 {{ font-size: 18px; }}
  h2 {{ font-size: 15px; margin-top: 24px; }}
  table {{ border-collapse: collapse; width: 100%; margin-bottom: 16px; }}
  th, td {{ border: 1px solid #999; padding: 4px 8px; text-align: left; }}
  th {{ background: #dce6f1; }}
</style>
</head>
<body>
<h1>NBA Criterion Report — Program {programId} — Academic Year {academicYear}</h1>
<h2>CO-PO Mapping Matrix</h2>
<table>
  <tr><th>Subject</th><th>CO</th><th>PO</th><th>Correlation Level</th></tr>
  {string.Join("", coPoRows.Select(m => $"<tr><td>{m.SubjectId}</td><td>{m.CourseOutcomeCode}</td><td>{m.ProgramOutcomeCode}</td><td>{m.CorrelationLevel}</td></tr>"))}
</table>
<h2>CO Attainment per Subject</h2>
<table>
  <tr><th>Subject</th><th>CO</th><th>Students Total</th><th>Students Attained</th><th>Attainment %</th><th>Level</th></tr>
  {string.Join("", daRows.Select(d => $"<tr><td>{d.SubjectId}</td><td>{d.CourseOutcomeCode}</td><td>{d.TotalStudents}</td><td>{d.StudentsAttained}</td><td>{d.AttainmentPercent:F2}%</td><td>{d.Level}</td></tr>"))}
</table>
<h2>PO Attainment Summary</h2>
<table>
  <tr><th>PO</th><th>Attainment %</th></tr>
  {string.Join("", poAttainments.OrderBy(p => p.Key).Select(p => $"<tr><td>{p.Key}</td><td>{p.Value:F2}%</td></tr>"))}
</table>
<h2>Gap Analysis</h2>
<table>
  <tr><th>Subject</th><th>CO</th><th>Direct %</th><th>Indirect %</th><th>Combined %</th><th>Target %</th><th>Gap %</th><th>Level</th></tr>
  {string.Join("", gaps.OrderBy(g => g.SubjectId).ThenBy(g => g.CourseOutcomeCode).Select(g => $"<tr><td>{g.SubjectId}</td><td>{g.CourseOutcomeCode}</td><td>{g.DirectAttainmentPercent:F2}%</td><td>{(g.IndirectAttainmentPercent.HasValue ? $"{g.IndirectAttainmentPercent:F2}%" : "-")}</td><td>{g.CombinedAttainmentPercent:F2}%</td><td>{g.TargetPercent:F2}%</td><td>{g.GapPercent:F2}%</td><td>{g.Level}</td></tr>"))}
</table>
<h2>Action Plans</h2>
<table>
  <tr><th>Subject</th><th>CO</th><th>Description</th><th>Status</th><th>Outcome</th></tr>
  {string.Join("", actionPlans.OrderBy(p => p.SubjectId).ThenBy(p => p.CourseOutcomeCode).Select(p => $"<tr><td>{p.SubjectId}</td><td>{p.CourseOutcomeCode}</td><td>{System.Net.WebUtility.HtmlEncode(p.Description)}</td><td>{p.Status}</td><td>{System.Net.WebUtility.HtmlEncode(p.Outcome ?? "")}</td></tr>"))}
</table>
</body>
</html>";

        return html;
    }
}
