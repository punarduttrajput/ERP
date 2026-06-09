using System.Text;
using System.Text.Json;
using ERP.NAAC.Domain;
using ERP.NAAC.Infrastructure;
using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.NAAC.Application.Commands;

public record GenerateSsrPdfCommand(Guid SsrId) : IRequest<Result<byte[]>>;

public class GenerateSsrPdfHandler : IRequestHandler<GenerateSsrPdfCommand, Result<byte[]>>
{
    private readonly INaacDbContext _db;
    private readonly IPdfService _pdfService;

    public GenerateSsrPdfHandler(INaacDbContext db, IPdfService pdfService)
    {
        _db = db;
        _pdfService = pdfService;
    }

    public async Task<Result<byte[]>> Handle(GenerateSsrPdfCommand request, CancellationToken cancellationToken)
    {
        var ssr = await _db.SsrReports
            .Include(r => r.Sections)
            .FirstOrDefaultAsync(r => r.Id == request.SsrId, cancellationToken);

        if (ssr is null)
            return Result<byte[]>.Failure("SSR not found.");

        var html = BuildHtml(ssr);
        var bytes = await _pdfService.GeneratePdfAsync(html, cancellationToken);
        return Result<byte[]>.Success(bytes);
    }

    private static string BuildHtml(SsrReport ssr)
    {
        var sb = new StringBuilder();
        sb.Append("<!DOCTYPE html><html><head><meta charset='utf-8'/><style>");
        sb.Append("body{font-family:Arial,sans-serif;margin:40px;}");
        sb.Append("h1{font-size:24px;text-align:center;}");
        sb.Append("h2{font-size:18px;border-bottom:2px solid #333;padding-bottom:4px;margin-top:32px;}");
        sb.Append("h3{font-size:14px;margin-top:20px;}");
        sb.Append(".content{margin:8px 0;white-space:pre-wrap;}");
        sb.Append(".metrics table{border-collapse:collapse;width:100%;}");
        sb.Append(".metrics td,.metrics th{border:1px solid #ccc;padding:6px 10px;}");
        sb.Append(".toc a{display:block;margin:2px 0;color:#0066cc;text-decoration:none;}");
        sb.Append("</style></head><body>");

        sb.Append($"<h1>{ssr.Title}</h1>");
        sb.Append("<h2>Table of Contents</h2><div class='toc'>");
        foreach (var criterion in NaacCriteria.All)
            sb.Append($"<a href='#c{criterion.Number}'>Criterion {criterion.Number}: {criterion.Title}</a>");
        sb.Append("</div>");

        foreach (var criterion in NaacCriteria.All)
        {
            sb.Append($"<h2 id='c{criterion.Number}'>Criterion {criterion.Number}: {criterion.Title}</h2>");
            foreach (var indicator in criterion.Indicators)
            {
                var section = ssr.Sections.FirstOrDefault(s => s.IndicatorNumber == indicator);
                sb.Append($"<h3>{indicator}</h3>");
                sb.Append($"<div class='content'>{System.Net.WebUtility.HtmlEncode(section?.Content ?? string.Empty)}</div>");

                if (section?.AutoMetrics is not null)
                {
                    sb.Append("<div class='metrics'>");
                    AppendMetricsTable(sb, section.AutoMetrics);
                    sb.Append("</div>");
                }
            }
        }

        sb.Append("</body></html>");
        return sb.ToString();
    }

    private static void AppendMetricsTable(StringBuilder sb, string autoMetricsJson)
    {
        try
        {
            var metrics = JsonSerializer.Deserialize<Dictionary<string, string>>(autoMetricsJson);
            if (metrics is null || metrics.Count == 0) return;

            sb.Append("<table><thead><tr><th>Metric</th><th>Value</th></tr></thead><tbody>");
            foreach (var kv in metrics)
                sb.Append($"<tr><td>{System.Net.WebUtility.HtmlEncode(kv.Key)}</td><td>{System.Net.WebUtility.HtmlEncode(kv.Value)}</td></tr>");
            sb.Append("</tbody></table>");
        }
        catch (JsonException)
        {
            // If AutoMetrics is not valid JSON, render as plain text
            sb.Append($"<p>{System.Net.WebUtility.HtmlEncode(autoMetricsJson)}</p>");
        }
    }
}
