namespace Jumbo.ProductCatalog.Core.Interfaces;

public interface IFileStorageService
{
    Task WriteAsync(string fileName, byte[] data, CancellationToken ct = default);
}
