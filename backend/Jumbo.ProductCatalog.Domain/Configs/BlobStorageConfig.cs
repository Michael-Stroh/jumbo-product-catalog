namespace Jumbo.ProductCatalog.Domain.Configs;

public sealed class BlobStorageConfig
{
    public const string SectionName = "BlobStorage";

    public string ConnectionString { get; init; } = string.Empty;
    public string ExportsContainerName { get; init; } = "product-catalog-exports";
}
