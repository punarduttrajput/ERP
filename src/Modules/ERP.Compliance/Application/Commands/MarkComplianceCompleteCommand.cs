using ERP.Compliance.Domain;
using ERP.Compliance.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Compliance.Application.Commands;

public record MarkComplianceCompleteCommand(
    Guid TenantId,
    Guid Id,
    Guid CompletedBy,
    string? SubmissionReference,
    string? Notes) : IRequest<Result>;

public class MarkComplianceCompleteHandler : IRequestHandler<MarkComplianceCompleteCommand, Result>
{
    private readonly IComplianceDbContext _db;

    public MarkComplianceCompleteHandler(IComplianceDbContext db)
    {
        _db = db;
    }

    public async Task<Result> Handle(MarkComplianceCompleteCommand request, CancellationToken cancellationToken)
    {
        var item = await _db.ComplianceItems
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.TenantId == request.TenantId && !x.IsDeleted, cancellationToken);

        if (item is null)
            return Result.Failure("Compliance item not found.");

        if (item.Status == ComplianceStatus.Completed)
            return Result.Failure("Compliance item is already marked as completed.");

        item.Status = ComplianceStatus.Completed;
        item.CompletedAt = DateTime.UtcNow;
        item.CompletedBy = request.CompletedBy;
        item.SubmissionReference = request.SubmissionReference;
        item.Notes = request.Notes;
        item.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
