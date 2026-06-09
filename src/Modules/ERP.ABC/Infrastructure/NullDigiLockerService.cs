using ERP.ABC.Domain;

namespace ERP.ABC.Infrastructure;

public sealed class NullDigiLockerService : IDigiLockerService
{
    public Task<(bool IsValid, string? StudentName)> VerifyAbcIdAsync(
        string abcId, CancellationToken ct = default)
    {
        var valid = abcId.Length == 12 && abcId.All(char.IsDigit);
        return Task.FromResult((valid, valid ? "Test Student (Dev)" : (string?)null));
    }

    public Task<IReadOnlyList<AbcCreditRecord>> GetCreditsAsync(
        string abcId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<AbcCreditRecord>>(Array.Empty<AbcCreditRecord>());
}
