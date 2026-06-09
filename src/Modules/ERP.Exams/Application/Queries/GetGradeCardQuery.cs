using ERP.Exams.Infrastructure;
using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Exams.Application.Queries;

public record GetGradeCardQuery(Guid StudentId, Guid SemesterId) : IRequest<Result<byte[]>>;

public class GetGradeCardHandler : IRequestHandler<GetGradeCardQuery, Result<byte[]>>
{
    private readonly IExamsDbContext _db;
    private readonly IPdfService _pdfService;

    public GetGradeCardHandler(IExamsDbContext db, IPdfService pdfService)
    {
        _db = db;
        _pdfService = pdfService;
    }

    public async Task<Result<byte[]>> Handle(GetGradeCardQuery request, CancellationToken cancellationToken)
    {
        var results = await _db.StudentResults
            .Where(r =>
                r.StudentId == request.StudentId &&
                r.SemesterId == request.SemesterId &&
                r.IsPublished)
            .OrderBy(r => r.SubjectName)
            .ToListAsync(cancellationToken);

        if (results.Count == 0)
            return Result<byte[]>.Failure("No published results found for this student and semester.");

        var gpa = results.First().GPA ?? 0m;
        var cgpa = results.First().CGPA ?? 0m;
        var publishedAt = results.First().PublishedAt?.ToString("dd MMM yyyy") ?? "-";

        var html = BuildGradeCardHtml(request.StudentId, request.SemesterId, results, gpa, cgpa, publishedAt);
        var pdfBytes = await _pdfService.GeneratePdfAsync(html, cancellationToken);

        return Result<byte[]>.Success(pdfBytes);
    }

    private static string BuildGradeCardHtml(
        Guid studentId,
        Guid semesterId,
        List<ERP.Exams.Domain.StudentResult> results,
        decimal gpa,
        decimal cgpa,
        string publishedAt)
    {
        var rows = string.Join(string.Empty, results.Select((r, i) => $@"
            <tr style=""background-color: {(i % 2 == 0 ? "#f9f9f9" : "#ffffff")};"">
                <td style=""padding: 8px 12px; border: 1px solid #ddd;"">{r.SubjectName}</td>
                <td style=""padding: 8px 12px; border: 1px solid #ddd; text-align: center;"">{r.InternalMarks:F1}</td>
                <td style=""padding: 8px 12px; border: 1px solid #ddd; text-align: center;"">{r.ExternalMarks:F1}</td>
                <td style=""padding: 8px 12px; border: 1px solid #ddd; text-align: center;"">{r.TotalMarks:F1} / {r.MaxMarks:F0}</td>
                <td style=""padding: 8px 12px; border: 1px solid #ddd; text-align: center;"">{r.GradeLetter}</td>
                <td style=""padding: 8px 12px; border: 1px solid #ddd; text-align: center;"">{r.GradePoints:F1}</td>
                <td style=""padding: 8px 12px; border: 1px solid #ddd; text-align: center;"">{r.Credits}</td>
                <td style=""padding: 8px 12px; border: 1px solid #ddd; text-align: center; color: {(r.Status == Domain.ResultStatus.Pass ? "#2e7d32" : "#c62828")};"">
                    {r.Status}
                </td>
            </tr>"));

        return $@"<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"" />
    <title>Grade Card</title>
</head>
<body style=""font-family: Arial, sans-serif; margin: 40px; color: #333;"">
    <div style=""text-align: center; border-bottom: 2px solid #1a237e; padding-bottom: 20px; margin-bottom: 30px;"">
        <h1 style=""color: #1a237e; margin: 0; font-size: 24px;"">University ERP Platform</h1>
        <h2 style=""color: #555; margin: 8px 0 0 0; font-size: 18px;"">Official Grade Card</h2>
    </div>

    <div style=""margin-bottom: 24px; padding: 16px; background-color: #e8eaf6; border-radius: 4px;"">
        <table style=""width: 100%; border-collapse: collapse;"">
            <tr>
                <td style=""width: 50%; padding: 4px 0;"">
                    <strong>Student ID:</strong> {studentId}
                </td>
                <td style=""width: 50%; padding: 4px 0;"">
                    <strong>Semester ID:</strong> {semesterId}
                </td>
            </tr>
            <tr>
                <td style=""padding: 4px 0;"">
                    <strong>Published On:</strong> {publishedAt}
                </td>
                <td style=""padding: 4px 0;"">
                    <strong>Total Subjects:</strong> {results.Count}
                </td>
            </tr>
        </table>
    </div>

    <table style=""width: 100%; border-collapse: collapse; margin-bottom: 24px;"">
        <thead>
            <tr style=""background-color: #1a237e; color: #ffffff;"">
                <th style=""padding: 10px 12px; border: 1px solid #ddd; text-align: left;"">Subject</th>
                <th style=""padding: 10px 12px; border: 1px solid #ddd; text-align: center;"">Internal</th>
                <th style=""padding: 10px 12px; border: 1px solid #ddd; text-align: center;"">External</th>
                <th style=""padding: 10px 12px; border: 1px solid #ddd; text-align: center;"">Total / Max</th>
                <th style=""padding: 10px 12px; border: 1px solid #ddd; text-align: center;"">Grade</th>
                <th style=""padding: 10px 12px; border: 1px solid #ddd; text-align: center;"">Grade Points</th>
                <th style=""padding: 10px 12px; border: 1px solid #ddd; text-align: center;"">Credits</th>
                <th style=""padding: 10px 12px; border: 1px solid #ddd; text-align: center;"">Status</th>
            </tr>
        </thead>
        <tbody>
            {rows}
        </tbody>
    </table>

    <div style=""padding: 16px; background-color: #e8eaf6; border-radius: 4px; text-align: right;"">
        <span style=""font-size: 16px; font-weight: bold; margin-right: 40px;"">
            Semester GPA: <span style=""color: #1a237e;"">{gpa:F2}</span>
        </span>
        <span style=""font-size: 16px; font-weight: bold;"">
            Cumulative CGPA: <span style=""color: #1a237e;"">{cgpa:F2}</span>
        </span>
    </div>

    <div style=""margin-top: 60px; text-align: right; font-size: 12px; color: #777;"">
        This is a computer-generated document and does not require a signature.
    </div>
</body>
</html>";
    }
}
