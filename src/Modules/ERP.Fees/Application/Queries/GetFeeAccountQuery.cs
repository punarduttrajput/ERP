using ERP.Fees.Domain;
using ERP.Fees.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Fees.Application.Queries;

public record GetFeeAccountQuery(Guid StudentId) : IRequest<Result<StudentFeeAccount>>;

public class GetFeeAccountHandler : IRequestHandler<GetFeeAccountQuery, Result<StudentFeeAccount>>
{
    private readonly IFeesDbContext _db;

    public GetFeeAccountHandler(IFeesDbContext db)
    {
        _db = db;
    }

    public async Task<Result<StudentFeeAccount>> Handle(GetFeeAccountQuery request, CancellationToken cancellationToken)
    {
        var account = await _db.StudentFeeAccounts
            .Include(a => a.Installments)
            .Include(a => a.Payments)
            .FirstOrDefaultAsync(a => a.StudentId == request.StudentId, cancellationToken);

        if (account is null)
            return Result<StudentFeeAccount>.Failure("Fee account not found.");

        return Result<StudentFeeAccount>.Success(account);
    }
}
