using ERP.Research.Domain;
using ERP.Research.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Research.Application.Commands;

public record RecordDisbursementCommand(
    Guid TenantId,
    Guid GrantId,
    decimal Amount,
    DateOnly DisbursedAt,
    string? Reference,
    string? Notes) : IRequest<Result<Guid>>;

public class RecordDisbursementHandler : IRequestHandler<RecordDisbursementCommand, Result<Guid>>
{
    private readonly IResearchDbContext _db;

    public RecordDisbursementHandler(IResearchDbContext db) => _db = db;

    public async Task<Result<Guid>> Handle(RecordDisbursementCommand request, CancellationToken cancellationToken)
    {
        if (request.Amount <= 0)
            return Result<Guid>.Failure("Disbursement amount must be positive.");

        var grant = await _db.Grants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == request.TenantId && x.Id == request.GrantId && !x.IsDeleted, cancellationToken);

        if (grant is null)
            return Result<Guid>.Failure("Grant not found.");

        if (grant.DisbursedAmount + request.Amount > grant.SanctionedAmount)
            return Result<Guid>.Failure($"Disbursement would exceed the sanctioned amount of {grant.SanctionedAmount:N2}. Available: {grant.SanctionedAmount - grant.DisbursedAmount:N2}.");

        var disbursement = new GrantDisbursement
        {
            TenantId = request.TenantId,
            GrantId = request.GrantId,
            Amount = request.Amount,
            DisbursedAt = request.DisbursedAt,
            Reference = request.Reference,
            Notes = request.Notes
        };

        await _db.GrantDisbursements.AddAsync(disbursement, cancellationToken);

        grant.DisbursedAmount += request.Amount;

        if (grant.Status == GrantStatus.Approved && grant.DisbursedAmount > 0)
            grant.Status = GrantStatus.Active;

        await _db.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(disbursement.Id);
    }
}
