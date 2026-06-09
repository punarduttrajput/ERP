using System.Text;
using ERP.Reporting.Domain;
using ERP.Shared.Application.Abstractions;

namespace ERP.Reporting.Application.Services;

public class PdfReportExporter : IReportExporter
{
    private readonly IPdfService _pdfService;

    public PdfReportExporter(IPdfService pdfService)
    {
        _pdfService = pdfService;
    }

    public ExportFormat Format => ExportFormat.Pdf;

    public async Task<byte[]> ExportAsync(
        string reportName,
        IReadOnlyList<string> columns,
        IReadOnlyList<IDictionary<string, object?>> rows,
        CancellationToken cancellationToken = default)
    {
        var sb = new StringBuilder();
        sb.Append("<!DOCTYPE html><html><head><meta charset='utf-8'/>");
        sb.Append("<style>body{font-family:Arial,sans-serif;font-size:11px;}");
        sb.Append("h1{font-size:14px;margin-bottom:8px;}");
        sb.Append("table{border-collapse:collapse;width:100%;}");
        sb.Append("th{background:#2c5f8a;color:#fff;padding:6px 8px;text-align:left;}");
        sb.Append("td{padding:5px 8px;border-bottom:1px solid #ddd;}");
        sb.Append("tr:nth-child(even){background:#f5f5f5;}</style></head><body>");
        sb.Append($"<h1>{System.Net.WebUtility.HtmlEncode(reportName)}</h1>");
        sb.Append("<table><thead><tr>");
        foreach (var col in columns)
            sb.Append($"<th>{System.Net.WebUtility.HtmlEncode(col)}</th>");
        sb.Append("</tr></thead><tbody>");
        foreach (var row in rows)
        {
            sb.Append("<tr>");
            foreach (var col in columns)
                sb.Append($"<td>{System.Net.WebUtility.HtmlEncode((row.TryGetValue(col, out var v) ? v : null)?.ToString() ?? "")}</td>");
            sb.Append("</tr>");
        }
        sb.Append("</tbody></table></body></html>");

        return await _pdfService.GeneratePdfAsync(sb.ToString(), cancellationToken);
    }
}
