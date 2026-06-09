namespace ERP.Shared.Application.Contracts;

public interface IFeeService
{
    Task<decimal> GetOutstandingBalanceAsync(Guid studentId, CancellationToken cancellationToken = default);
    Task<bool> HasClearedFeesAsync(Guid studentId, Guid semesterId, CancellationToken cancellationToken = default);
}
