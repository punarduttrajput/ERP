namespace ERP.Shared.Application.Contracts;

/// <summary>
/// Implemented by every module that can supply evidence to accreditation engines.
/// Consume via IEnumerable<IEvidenceProvider> injected from DI.
/// </summary>
public interface IEvidenceProvider
{
    string ModuleName { get; }

    Task<IReadOnlyList<EvidenceItem>> GetEvidenceAsync(
        Guid tenantId,
        int academicYear,
        CancellationToken cancellationToken = default);
}

public record EvidenceItem(
    string Module,
    string Category,
    string Key,
    string Label,
    decimal? NumericValue,
    string? TextValue,
    DateTime RecordedAt,
    IDictionary<string, string>? Metadata = null
);
