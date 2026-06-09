namespace ERP.ABC.Domain;

public interface IDigiLockerService
{
    Task<(bool IsValid, string? StudentName)> VerifyAbcIdAsync(
        string abcId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AbcCreditRecord>> GetCreditsAsync(
        string abcId, CancellationToken cancellationToken = default);
}

public record AbcCreditRecord(
    string InstitutionName,
    string SubjectCode,
    string SubjectName,
    int Credits,
    string Grade,
    int AcademicYear);
