using ERP.NAAC.Domain;
using ERP.NAAC.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.NAAC.Application.Queries;

public record GetAqarQuery(Guid AqarId) : IRequest<Result<AqarReportDto>>;

public record AqarReportDto(
    Guid Id,
    int AcademicYear,
    string Title,
    AqarStatus Status,
    DateTime? SubmittedAt,
    IReadOnlyList<AqarSectionDto> Sections);

public record AqarSectionDto(
    Guid Id,
    string CriterionNumber,
    string Title,
    Guid? AssignedTo,
    string? Content,
    AqarStatus Status,
    string? ReviewComment,
    Guid? ReviewedBy,
    DateTime? ReviewedAt);

public class GetAqarHandler : IRequestHandler<GetAqarQuery, Result<AqarReportDto>>
{
    private readonly INaacDbContext _db;

    public GetAqarHandler(INaacDbContext db) => _db = db;

    public async Task<Result<AqarReportDto>> Handle(GetAqarQuery request, CancellationToken cancellationToken)
    {
        var aqar = await _db.AqarReports
            .Include(a => a.Sections)
            .FirstOrDefaultAsync(a => a.Id == request.AqarId, cancellationToken);

        if (aqar is null)
            return Result<AqarReportDto>.Failure("AQAR not found.");

        var dto = new AqarReportDto(
            aqar.Id,
            aqar.AcademicYear,
            aqar.Title,
            aqar.Status,
            aqar.SubmittedAt,
            aqar.Sections.Select(s => new AqarSectionDto(
                s.Id, s.CriterionNumber, s.Title, s.AssignedTo,
                s.Content, s.Status, s.ReviewComment, s.ReviewedBy, s.ReviewedAt))
            .ToList());

        return Result<AqarReportDto>.Success(dto);
    }
}
