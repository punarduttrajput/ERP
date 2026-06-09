using ERP.NAAC.Domain;
using ERP.NAAC.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;

namespace ERP.NAAC.Application.Commands;

public record CreateSsrCommand(Guid TenantId, int AcademicYear, string Title) : IRequest<Result<Guid>>;

public class CreateSsrHandler : IRequestHandler<CreateSsrCommand, Result<Guid>>
{
    private readonly INaacDbContext _db;

    public CreateSsrHandler(INaacDbContext db) => _db = db;

    public async Task<Result<Guid>> Handle(CreateSsrCommand request, CancellationToken cancellationToken)
    {
        var ssr = new SsrReport
        {
            TenantId = request.TenantId,
            AcademicYear = request.AcademicYear,
            Title = request.Title,
            Status = SsrStatus.Draft
        };

        foreach (var criterion in NaacCriteria.All)
        {
            foreach (var indicator in criterion.Indicators)
            {
                ssr.Sections.Add(new SsrSection
                {
                    TenantId = request.TenantId,
                    SsrId = ssr.Id,
                    CriterionNumber = criterion.Number,
                    IndicatorNumber = indicator,
                    Title = $"{indicator} — {criterion.Title}",
                    Content = string.Empty,
                    AutoMetrics = null
                });
            }
        }

        _db.SsrReports.Add(ssr);
        await _db.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(ssr.Id);
    }
}
