using Dapper;
using ERP.NIRF.Infrastructure;
using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace ERP.NIRF.Application.Queries;

public record ExportNirfPdfQuery(Guid TenantId, Guid SubmissionId) : IRequest<Result<byte[]>>;

public class ExportNirfPdfHandler : IRequestHandler<ExportNirfPdfQuery, Result<byte[]>>
{
    private readonly INirfDbContext _db;
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IPdfService _pdfService;

    public ExportNirfPdfHandler(INirfDbContext db, IDbConnectionFactory connectionFactory, IPdfService pdfService)
    {
        _db = db;
        _connectionFactory = connectionFactory;
        _pdfService = pdfService;
    }

    public async Task<Result<byte[]>> Handle(ExportNirfPdfQuery request, CancellationToken cancellationToken)
    {
        var submission = _db.NirfSubmissions
            .Include(s => s.ParameterScores.Where(p => !p.IsDeleted))
            .FirstOrDefault(s => s.Id == request.SubmissionId && s.TenantId == request.TenantId && !s.IsDeleted);

        if (submission is null)
            return Result.Failure<byte[]>("Submission not found.");

        using var conn = await _connectionFactory.CreateReadConnectionAsync(cancellationToken);
        var tenantName = await conn.ExecuteScalarAsync<string>(
            "SELECT Name FROM tenants WHERE Id = @TenantId AND IsDeleted = 0",
            new { request.TenantId }) ?? "Unknown Institution";

        var paramRows = new StringBuilder();
        foreach (var p in submission.ParameterScores.OrderBy(x => x.Parameter))
        {
            paramRows.Append("<tr>");
            paramRows.Append($"<td>{p.Parameter}</td>");
            paramRows.Append($"<td>{p.Weight:P0}</td>");
            paramRows.Append($"<td>{p.RawScore:F2}</td>");
            paramRows.Append($"<td>{p.WeightedScore:F2}</td>");
            paramRows.Append($"<td>{(p.IsManualOverride ? "Manual" : "Compiled")}</td>");
            paramRows.Append("</tr>");
        }

        var subMetricRows = new StringBuilder();
        foreach (var p in submission.ParameterScores.OrderBy(x => x.Parameter))
        {
            subMetricRows.Append("<tr>");
            subMetricRows.Append($"<td><strong>{p.Parameter}</strong></td>");
            subMetricRows.Append($"<td colspan=\"4\"><pre style=\"font-size:10px;white-space:pre-wrap\">{System.Net.WebUtility.HtmlEncode(p.DataJson)}</pre></td>");
            subMetricRows.Append("</tr>");
        }

        var submittedRow = submission.SubmittedAt.HasValue
            ? $"<p><strong>Submitted At:</strong> {submission.SubmittedAt:yyyy-MM-dd}</p>"
            : string.Empty;

        var rankRow = submission.EstimatedRank.HasValue
            ? $"<br/><strong>Estimated Rank:</strong> {submission.EstimatedRank}"
            : string.Empty;

        var html = new StringBuilder();
        html.Append("<!DOCTYPE html><html><head><meta charset=\"UTF-8\"/><style>");
        html.Append("body { font-family: Arial, sans-serif; margin: 40px; color: #333; }");
        html.Append("h1 { color: #1a3a5c; border-bottom: 2px solid #1a3a5c; padding-bottom: 10px; }");
        html.Append("h2 { color: #1a3a5c; margin-top: 30px; }");
        html.Append("table { width: 100%; border-collapse: collapse; margin-top: 10px; }");
        html.Append("th { background: #1a3a5c; color: white; padding: 8px; text-align: left; }");
        html.Append("td { padding: 8px; border: 1px solid #ddd; }");
        html.Append("tr:nth-child(even) { background: #f5f5f5; }");
        html.Append(".summary { background: #e8f4fd; padding: 15px; border-radius: 5px; margin: 20px 0; }");
        html.Append(".score-big { font-size: 36px; font-weight: bold; color: #1a3a5c; }");
        html.Append("</style></head><body>");
        html.Append("<h1>NIRF Submission Report</h1>");
        html.Append("<div class=\"summary\">");
        html.Append($"<p><strong>Institution:</strong> {tenantName}</p>");
        html.Append($"<p><strong>Ranking Year:</strong> {submission.RankingYear}</p>");
        html.Append($"<p><strong>Category:</strong> {submission.Category}</p>");
        html.Append($"<p><strong>Status:</strong> {submission.Status}</p>");
        html.Append(submittedRow);
        html.Append("</div>");
        html.Append("<h2>Overall Score</h2><div class=\"summary\">");
        html.Append($"<span class=\"score-big\">{submission.OverallScore?.ToString("F2") ?? "N/A"}</span> / 100");
        html.Append(rankRow);
        html.Append("</div>");
        html.Append("<h2>Parameter Scores</h2><table><thead><tr>");
        html.Append("<th>Parameter</th><th>Weight</th><th>Raw Score (0-100)</th><th>Weighted Score</th><th>Source</th>");
        html.Append("</tr></thead><tbody>");
        html.Append(paramRows);
        html.Append("</tbody></table>");
        html.Append("<h2>Sub-Metrics Breakdown</h2><table><thead><tr>");
        html.Append("<th>Parameter</th><th colspan=\"4\">Data</th></tr></thead><tbody>");
        html.Append(subMetricRows);
        html.Append("</tbody></table></body></html>");

        var bytes = await _pdfService.GeneratePdfAsync(html.ToString(), cancellationToken);
        return Result.Success(bytes);
    }
}
