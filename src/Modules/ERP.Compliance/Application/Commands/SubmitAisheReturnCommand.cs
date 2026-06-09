using ERP.Compliance.Domain;
using ERP.Compliance.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;

namespace ERP.Compliance.Application.Commands;

public record SubmitAisheReturnCommand(Guid TenantId, int AcademicYear, string SubmissionReference) : IRequest<Result>;

public class SubmitAisheReturnHandler : IRequestHandler<SubmitAisheReturnCommand, Result>
{
    private readonly IComplianceDbContext _db;

    public SubmitAisheReturnHandler(IComplianceDbContext db)
    {
        _db = db;
    }

    public async Task<Result> Handle(SubmitAisheReturnCommand request, CancellationToken cancellationToken)
    {
        var aisheReturn = _db.AisheReturns
            .FirstOrDefault(r => r.TenantId == request.TenantId && r.AcademicYear == request.AcademicYear && !r.IsDeleted);

        if (aisheReturn is null)
            return Result.Failure("AISHE return not found. Compile it first.");

        if (aisheReturn.Status == AisheReturnStatus.Draft)
            return Result.Failure("AISHE return must be compiled before submission.");

        aisheReturn.Status = AisheReturnStatus.Submitted;
        aisheReturn.SubmittedAt = DateTime.UtcNow;
        aisheReturn.SubmissionReference = request.SubmissionReference;
        aisheReturn.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
