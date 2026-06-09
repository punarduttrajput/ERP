using ERP.Reporting.Domain;

namespace ERP.Reporting.Application.Services;

public interface IReportExporter
{
    ExportFormat Format { get; }
    Task<byte[]> ExportAsync(
        string reportName,
        IReadOnlyList<string> columns,
        IReadOnlyList<IDictionary<string, object?>> rows,
        CancellationToken cancellationToken = default);
}
