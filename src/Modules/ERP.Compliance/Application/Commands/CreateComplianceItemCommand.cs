using ERP.Compliance.Domain;
using ERP.Compliance.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;

namespace ERP.Compliance.Application.Commands;

public record CreateComplianceItemCommand(
    Guid TenantId,
    ComplianceAuthority Authority,
    string Title,
    string? Description,
    DateOnly DueDate,
    Guid? ResponsiblePersonId,
    string? ResponsiblePersonName,
    int AcademicYear,
    bool IsRecurring = false,
    string? RecurrencePattern = null) : IRequest<Result<Guid>>;

public class CreateComplianceItemHandler : IRequestHandler<CreateComplianceItemCommand, Result<Guid>>
{
    private readonly IComplianceDbContext _db;

    public CreateComplianceItemHandler(IComplianceDbContext db)
    {
        _db = db;
    }

    public async Task<Result<Guid>> Handle(CreateComplianceItemCommand request, CancellationToken cancellationToken)
    {
        var item = new ComplianceItem
        {
            TenantId = request.TenantId,
            Authority = request.Authority,
            Title = request.Title,
            Description = request.Description,
            DueDate = request.DueDate,
            ResponsiblePersonId = request.ResponsiblePersonId,
            ResponsiblePersonName = request.ResponsiblePersonName,
            AcademicYear = request.AcademicYear,
            IsRecurring = request.IsRecurring,
            RecurrencePattern = request.RecurrencePattern,
            Status = ComplianceStatus.Pending
        };

        _db.ComplianceItems.Add(item);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(item.Id);
    }
}
