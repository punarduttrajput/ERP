using ERP.ABC.Domain;
using ERP.ABC.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.ABC.Application.Commands;

public record RejectCreditTransferCommand(
    Guid TenantId,
    Guid TransferId,
    Guid RejectedBy,
    string RejectionReason) : IRequest<Result>;

public class RejectCreditTransferHandler : IRequestHandler<RejectCreditTransferCommand, Result>
{
    private readonly IAbcDbContext _db;

    public RejectCreditTransferHandler(IAbcDbContext db) => _db = db;

    public async Task<Result> Handle(RejectCreditTransferCommand request, CancellationToken cancellationToken)
    {
        var transfer = await _db.CreditTransfers
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == request.TransferId && x.TenantId == request.TenantId && !x.IsDeleted, cancellationToken);

        if (transfer is null)
            return Result.Failure("Credit transfer not found.");
        if (transfer.Status != TransferStatus.Pending)
            return Result.Failure("Only pending transfers can be rejected.");

        transfer.Status = TransferStatus.Rejected;
        transfer.ApprovedBy = request.RejectedBy;
        transfer.ApprovedAt = DateTime.UtcNow;
        transfer.RejectionReason = request.RejectionReason;

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
