using ERP.Fees.Domain;
using ERP.Fees.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Fees.Application.Queries;

public record GetPaymentHistoryQuery(Guid StudentId) : IRequest<Result<IReadOnlyList<FeePayment>>>;

public class GetPaymentHistoryHandler : IRequestHandler<GetPaymentHistoryQuery, Result<IReadOnlyList<FeePayment>>>
{
    private readonly IFeesDbContext _db;

    public GetPaymentHistoryHandler(IFeesDbContext db)
    {
        _db = db;
    }

    public async Task<Result<IReadOnlyList<FeePayment>>> Handle(GetPaymentHistoryQuery request, CancellationToken cancellationToken)
    {
        var accounts = await _db.StudentFeeAccounts
            .Where(a => a.StudentId == request.StudentId)
            .Select(a => a.Id)
            .ToListAsync(cancellationToken);

        var payments = await _db.FeePayments
            .Where(p => accounts.Contains(p.AccountId))
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<FeePayment>>.Success(payments);
    }
}
