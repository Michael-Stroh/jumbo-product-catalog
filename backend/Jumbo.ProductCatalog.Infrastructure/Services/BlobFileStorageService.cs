using Azure.Storage.Blobs;
using Jumbo.ProductCatalog.Core.Interfaces;
using Jumbo.ProductCatalog.Domain.Configs;
using Microsoft.Extensions.Options;

namespace Jumbo.ProductCatalog.Infrastructure.Services;

public sealed class BlobFileStorageService(IOptions<BlobStorageConfig> config) : IFileStorageService
{
    public async Task WriteAsync(string fileName, byte[] data, CancellationToken ct = default)
    {
        var cfg = config.Value;
        var serviceClient = new BlobServiceClient(cfg.ConnectionString);
        var containerClient = serviceClient.GetBlobContainerClient(cfg.ExportsContainerName);
        await containerClient.CreateIfNotExistsAsync(cancellationToken: ct);

        var blobClient = containerClient.GetBlobClient(fileName);
        using var stream = new MemoryStream(data);
        await blobClient.UploadAsync(stream, overwrite: true, cancellationToken: ct);
    }
}
