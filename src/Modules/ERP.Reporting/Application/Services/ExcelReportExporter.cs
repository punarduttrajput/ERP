using ClosedXML.Excel;
using ERP.Reporting.Domain;

namespace ERP.Reporting.Application.Services;

public class ExcelReportExporter : IReportExporter
{
    public ExportFormat Format => ExportFormat.Excel;

    public Task<byte[]> ExportAsync(
        string reportName,
        IReadOnlyList<string> columns,
        IReadOnlyList<IDictionary<string, object?>> rows,
        CancellationToken cancellationToken = default)
    {
        var workbook = new XLWorkbook();
        var sheetName = reportName[..Math.Min(31, reportName.Length)];
        var sheet = workbook.Worksheets.Add(sheetName);

        for (var i = 0; i < columns.Count; i++)
            sheet.Cell(1, i + 1).Value = columns[i];
        sheet.Row(1).Style.Font.Bold = true;

        for (var r = 0; r < rows.Count; r++)
            for (var c = 0; c < columns.Count; c++)
                sheet.Cell(r + 2, c + 1).Value = (rows[r].TryGetValue(columns[c], out var v) ? v : null)?.ToString() ?? "";

        sheet.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return Task.FromResult(ms.ToArray());
    }
}
