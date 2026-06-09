using ERP.Compliance.Domain;
using ERP.Compliance.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Compliance.Application.Queries;

public record GetUpcomingDeadlinesQuery(Guid TenantId) : IRequest<Result<IReadOnlyList<ComplianceItemDto>>>;

public class GetUpcomingDeadlinesHandler : IRequestHandler<GetUpcomingDeadlinesQuery, Result<IReadOnlyList<ComplianceItemDto>>>
{
    private readonly IComplianceDbContext _db;

    public GetUpcomingDeadlinesHandler(IComplianceDbContext db)
    {
        _db = db;
    }

    public async Task<Result<IReadOnlyList<ComplianceItemDto>>> Handle(GetUpcomingDeadlinesQuery request, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var cutoff = today.AddDays(30);

        var items = await _db.ComplianceItems
            .Where(x => x.TenantId == request.TenantId
                        && !x.IsDeleted
                        && x.DueDate >= today
                        && x.DueDate <= cutoff
                        && x.Status != ComplianceStatus.Completed
                        && x.Status != ComplianceStatus.NotApplicable)
            .OrderBy(x => x.DueDate)
            .Select(x => new ComplianceItemDto(
                x.Id, x.Authority, x.Title, x.Description, x.DueDate,
                x.ResponsiblePersonId, x.ResponsiblePersonName, x.Status,
                x.CompletedAt, x.SubmissionReference, x.Notes, x.AcademicYear,
                x.IsRecurring, x.RecurrencePattern, x.CreatedAt, x.UpdatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<ComplianceItemDto>>(items);
    }
}
