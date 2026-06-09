namespace ERP.Shared.Application.Abstractions;

public interface IAzureBlobService
{
    Task<string> UploadAsync(string container, string blobName, byte[] content, string contentType, CancellationToken ct = default);
    Task<byte[]> DownloadAsync(string container, string blobName, CancellationToken ct = default);
    Task DeleteAsync(string container, string blobName, CancellationToken ct = default);
}
