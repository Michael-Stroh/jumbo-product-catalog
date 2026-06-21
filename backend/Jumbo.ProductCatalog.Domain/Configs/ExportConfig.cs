namespace Jumbo.ProductCatalog.Domain.Configs;

public sealed class ExportConfig
{
    public const string SectionName = "Export";

    public int IntervalMinutes { get; init; } = 5;
}
