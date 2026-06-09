using ERP.Compliance.Domain;
using ERP.Compliance.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Compliance.Application.Commands;

public record UpdateComplianceItemCommand(
    Guid TenantId,
    Guid Id,
    ComplianceAuthority Authority,
    string Title,
    string? Description,
    DateOnly DueDate,
    Guid? ResponsiblePersonId,
    string? ResponsiblePersonName,
    ComplianceStatus Status,
    int AcademicYear,
    bool IsRecurring,
    string? RecurrencePattern) : IRequest<Result>;

public class UpdateComplianceItemHandler : IRequestHandler<UpdateComplianceItemCommand, Result>
{
    private readonly IComplianceDbContext _db;

    public UpdateComplianceItemHandler(IComplianceDbContext db)
    {
        _db = db;
    }

    public async Task<Result> Handle(UpdateComplianceItemCommand request, CancellationToken cancellationToken)
    {
        var item = await _db.ComplianceItems
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.TenantId == request.TenantId && !x.IsDeleted, cancellationToken);

        if (item is null)
            return Result.Failure("Compliance item not found.");

        item.Authority = request.Authority;
        item.Title = request.Title;
        item.Description = request.Description;
        item.DueDate = request.DueDate;
        item.ResponsiblePersonId = request.ResponsiblePersonId;
        item.ResponsiblePersonName = request.ResponsiblePersonName;
        item.Status = request.Status;
        item.AcademicYear = request.AcademicYear;
        item.IsRecurring = request.IsRecurring;
        item.RecurrencePattern = request.RecurrencePattern;
        item.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
