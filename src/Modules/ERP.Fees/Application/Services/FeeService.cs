using ERP.Fees.Infrastructure;
using ERP.Shared.Application.Contracts;
using Microsoft.EntityFrameworkCore;

namespace ERP.Fees.Application.Services;

public class FeeService : IFeeService
{
    private readonly IFeesDbContext _db;

    public FeeService(IFeesDbContext db)
    {
        _db = db;
    }

    public async Task<decimal> GetOutstandingBalanceAsync(Guid studentId, CancellationToken cancellationToken = default)
        => await _db.StudentFeeAccounts
            .Where(a => a.StudentId == studentId && !a.IsFullyPaid)
            .SumAsync(a => a.DueAmount, cancellationToken);

    public async Task<bool> HasClearedFeesAsync(Guid studentId, Guid semesterId, CancellationToken cancellationToken = default)
        => !await _db.StudentFeeAccounts
            .AnyAsync(a => a.StudentId == studentId && !a.IsFullyPaid && a.DueAmount > 0, cancellationToken);

    public async Task<bool> HasDuesAsync(Guid studentId, CancellationToken cancellationToken = default)
        => await _db.StudentFeeAccounts
            .AnyAsync(a => a.StudentId == studentId && !a.IsFullyPaid && a.DueAmount > 0, cancellationToken);
}
