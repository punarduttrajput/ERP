using ERP.Reporting.Application.Services;
using ERP.Reporting.Domain;
using ERP.Shared.Application.Common;
using MediatR;

namespace ERP.Reporting.Application.Commands;

public record ExportReportCommand(
    Guid TenantId,
    Guid? ReportDefinitionId,
    string? ReportCode,
    string? FiltersJson,
    ExportFormat Format,
    string? FileName,
    Guid? ExecutedBy) : IRequest<Result<ExportResultDto>>;

public record ExportResultDto(byte[] Content, string ContentType, string FileName);

public class ExportReportHandler : IRequestHandler<ExportReportCommand, Result<ExportResultDto>>
{
    private readonly IMediator _mediator;
    private readonly IEnumerable<IReportExporter> _exporters;

    public ExportReportHandler(IMediator mediator, IEnumerable<IReportExporter> exporters)
    {
        _mediator = mediator;
        _exporters = exporters;
    }

    public async Task<Result<ExportResultDto>> Handle(ExportReportCommand request, CancellationToken cancellationToken)
    {
        var executeResult = await _mediator.Send(new ExecuteReportCommand(
            request.TenantId,
            request.ReportDefinitionId,
            request.ReportCode,
            request.FiltersJson,
            request.ExecutedBy), cancellationToken);

        if (!executeResult.IsSuccess)
            return Result<ExportResultDto>.Failure(executeResult.Error!);

        var data = executeResult.Value!;
        var exporter = _exporters.FirstOrDefault(e => e.Format == request.Format);
        if (exporter is null)
            return Result<ExportResultDto>.Failure($"No exporter found for format {request.Format}.");

        var bytes = await exporter.ExportAsync(data.ReportName, data.Columns, data.Rows, cancellationToken);

        var (contentType, ext) = request.Format switch
        {
            ExportFormat.Pdf => ("application/pdf", "pdf"),
            ExportFormat.Excel => ("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "xlsx"),
            ExportFormat.Csv => ("text/csv", "csv"),
            _ => ("application/octet-stream", "bin")
        };

        var fileName = request.FileName ?? $"{data.ReportName.Replace(" ", "_")}_{DateTime.UtcNow:yyyyMMddHHmmss}.{ext}";

        return Result<ExportResultDto>.Success(new ExportResultDto(bytes, contentType, fileName));
    }
}
