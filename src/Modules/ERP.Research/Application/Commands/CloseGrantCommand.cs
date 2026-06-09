using ERP.Research.Domain;
using ERP.Research.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Research.Application.Commands;

public record CloseGrantCommand(
    Guid TenantId,
    Guid GrantId,
    decimal UtilizedAmount,
    string UtilizationCertificateReference) : IRequest<Result>;

public class CloseGrantHandler : IRequestHandler<CloseGrantCommand, Result>
{
    private readonly IResearchDbContext _db;

    public CloseGrantHandler(IResearchDbContext db) => _db = db;

    public async Task<Result> Handle(CloseGrantCommand request, CancellationToken cancellationToken)
    {
        var grant = await _db.Grants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == request.TenantId && x.Id == request.GrantId && !x.IsDeleted, cancellationToken);

        if (grant is null)
            return Result.Failure("Grant not found.");

        if (request.UtilizedAmount > grant.DisbursedAmount)
            return Result.Failure($"Utilized amount ({request.UtilizedAmount:N2}) cannot exceed disbursed amount ({grant.DisbursedAmount:N2}).");

        grant.UtilizedAmount = request.UtilizedAmount;
        grant.Status = GrantStatus.Closed;

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
