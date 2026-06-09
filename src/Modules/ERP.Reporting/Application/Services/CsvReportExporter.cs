using System.Text;
using ERP.Reporting.Domain;

namespace ERP.Reporting.Application.Services;

public class CsvReportExporter : IReportExporter
{
    public ExportFormat Format => ExportFormat.Csv;

    public Task<byte[]> ExportAsync(
        string reportName,
        IReadOnlyList<string> columns,
        IReadOnlyList<IDictionary<string, object?>> rows,
        CancellationToken cancellationToken = default)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",", columns.Select(c => $"\"{c}\"")));
        foreach (var row in rows)
            sb.AppendLine(string.Join(",", columns.Select(c =>
                $"\"{(row.TryGetValue(c, out var v) ? v : null)?.ToString()?.Replace("\"", "\"\"") ?? ""}\"")));
        return Task.FromResult(Encoding.UTF8.GetBytes(sb.ToString()));
    }
}
