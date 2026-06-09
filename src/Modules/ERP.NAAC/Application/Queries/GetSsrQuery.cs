using ERP.NAAC.Domain;
using ERP.NAAC.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.NAAC.Application.Queries;

public record GetSsrQuery(Guid SsrId) : IRequest<Result<SsrReportDto>>;

public record SsrReportDto(
    Guid Id,
    int AcademicYear,
    string Title,
    SsrStatus Status,
    DateTime? SubmittedAt,
    IReadOnlyList<SsrSectionDto> Sections);

public record SsrSectionDto(
    Guid Id,
    string CriterionNumber,
    string IndicatorNumber,
    string Title,
    string Content,
    string? AutoMetrics,
    Guid? LastEditedBy,
    DateTime? LastEditedAt);

public class GetSsrHandler : IRequestHandler<GetSsrQuery, Result<SsrReportDto>>
{
    private readonly INaacDbContext _db;

    public GetSsrHandler(INaacDbContext db) => _db = db;

    public async Task<Result<SsrReportDto>> Handle(GetSsrQuery request, CancellationToken cancellationToken)
    {
        var ssr = await _db.SsrReports
            .Include(r => r.Sections)
            .FirstOrDefaultAsync(r => r.Id == request.SsrId, cancellationToken);

        if (ssr is null)
            return Result<SsrReportDto>.Failure("SSR not found.");

        var dto = new SsrReportDto(
            ssr.Id,
            ssr.AcademicYear,
            ssr.Title,
            ssr.Status,
            ssr.SubmittedAt,
            ssr.Sections.Select(s => new SsrSectionDto(
                s.Id, s.CriterionNumber, s.IndicatorNumber, s.Title,
                s.Content, s.AutoMetrics, s.LastEditedBy, s.LastEditedAt))
            .ToList());

        return Result<SsrReportDto>.Success(dto);
    }
}
