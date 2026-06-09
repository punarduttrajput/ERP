namespace ERP.Shared.Application.Abstractions;

public interface IPdfService
{
    Task<byte[]> GeneratePdfAsync(string html, CancellationToken cancellationToken = default);
}
