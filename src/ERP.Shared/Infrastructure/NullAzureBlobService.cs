using ERP.Shared.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace ERP.Shared.Infrastructure;

public class NullAzureBlobService : IAzureBlobService
{
    private readonly ILogger<NullAzureBlobService> _logger;

    public NullAzureBlobService(ILogger<NullAzureBlobService> logger)
    {
        _logger = logger;
    }

    public Task<string> UploadAsync(string container, string blobName, byte[] content, string contentType, CancellationToken ct = default)
    {
        var url = $"https://null-blob/{container}/{blobName}";
        _logger.LogInformation("NullAzureBlobService: simulated upload {Bytes} bytes to {Url}", content.Length, url);
        return Task.FromResult(url);
    }

    public Task<byte[]> DownloadAsync(string container, string blobName, CancellationToken ct = default)
    {
        _logger.LogInformation("NullAzureBlobService: simulated download from {Container}/{BlobName}", container, blobName);
        return Task.FromResult(Array.Empty<byte>());
    }

    public Task DeleteAsync(string container, string blobName, CancellationToken ct = default)
    {
        _logger.LogInformation("NullAzureBlobService: simulated delete {Container}/{BlobName}", container, blobName);
        return Task.CompletedTask;
    }
}
