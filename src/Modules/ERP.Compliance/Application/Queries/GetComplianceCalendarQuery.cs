using ERP.Compliance.Domain;
using ERP.Compliance.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Compliance.Application.Queries;

public record GetComplianceCalendarQuery(
    Guid TenantId,
    int Page,
    int PageSize,
    ComplianceAuthority? Authority = null,
    ComplianceStatus? Status = null,
    int? Month = null,
    int? AcademicYear = null) : IRequest<Result<PagedResult<ComplianceItemDto>>>;

public record ComplianceItemDto(
    Guid Id,
    ComplianceAuthority Authority,
    string Title,
    string? Description,
    DateOnly DueDate,
    Guid? ResponsiblePersonId,
    string? ResponsiblePersonName,
    ComplianceStatus Status,
    DateTime? CompletedAt,
    string? SubmissionReference,
    string? Notes,
    int AcademicYear,
    bool IsRecurring,
    string? RecurrencePattern,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public class GetComplianceCalendarHandler : IRequestHandler<GetComplianceCalendarQuery, Result<PagedResult<ComplianceItemDto>>>
{
    private readonly IComplianceDbContext _db;

    public GetComplianceCalendarHandler(IComplianceDbContext db)
    {
        _db = db;
    }

    public async Task<Result<PagedResult<ComplianceItemDto>>> Handle(GetComplianceCalendarQuery request, CancellationToken cancellationToken)
    {
        var query = _db.ComplianceItems
            .Where(x => x.TenantId == request.TenantId && !x.IsDeleted);

        if (request.Authority.HasValue)
            query = query.Where(x => x.Authority == request.Authority.Value);

        if (request.Status.HasValue)
            query = query.Where(x => x.Status == request.Status.Value);

        if (request.Month.HasValue)
            query = query.Where(x => x.DueDate.Month == request.Month.Value);

        if (request.AcademicYear.HasValue)
            query = query.Where(x => x.AcademicYear == request.AcademicYear.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(x => x.DueDate)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new ComplianceItemDto(
                x.Id, x.Authority, x.Title, x.Description, x.DueDate,
                x.ResponsiblePersonId, x.ResponsiblePersonName, x.Status,
                x.CompletedAt, x.SubmissionReference, x.Notes, x.AcademicYear,
                x.IsRecurring, x.RecurrencePattern, x.CreatedAt, x.UpdatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success(new PagedResult<ComplianceItemDto>(items, totalCount, request.Page, request.PageSize));
    }
}
