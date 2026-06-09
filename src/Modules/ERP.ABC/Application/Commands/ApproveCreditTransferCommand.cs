using ERP.ABC.Domain;
using ERP.ABC.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.ABC.Application.Commands;

public record ApproveCreditTransferCommand(
    Guid TenantId,
    Guid TransferId,
    int CreditsApproved,
    Guid ApprovedBy,
    string? AbcRegistryReference) : IRequest<Result>;

public class ApproveCreditTransferHandler : IRequestHandler<ApproveCreditTransferCommand, Result>
{
    private readonly IAbcDbContext _db;

    public ApproveCreditTransferHandler(IAbcDbContext db) => _db = db;

    public async Task<Result> Handle(ApproveCreditTransferCommand request, CancellationToken cancellationToken)
    {
        var transfer = await _db.CreditTransfers
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == request.TransferId && x.TenantId == request.TenantId && !x.IsDeleted, cancellationToken);

        if (transfer is null)
            return Result.Failure("Credit transfer not found.");
        if (transfer.Status != TransferStatus.Pending)
            return Result.Failure("Only pending transfers can be approved.");

        transfer.Status = TransferStatus.Approved;
        transfer.CreditsApproved = request.CreditsApproved;
        transfer.ApprovedBy = request.ApprovedBy;
        transfer.ApprovedAt = DateTime.UtcNow;
        transfer.AbcRegistryReference = request.AbcRegistryReference;

        var profile = await _db.StudentAbcProfiles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == request.TenantId && x.StudentId == transfer.StudentId && !x.IsDeleted, cancellationToken);

        if (profile is not null)
        {
            if (transfer.Direction == TransferDirection.Incoming)
                profile.TotalCreditsTransferredIn += request.CreditsApproved;
            else
                profile.TotalCreditsTransferredOut += request.CreditsApproved;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
