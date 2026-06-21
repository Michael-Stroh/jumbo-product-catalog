namespace Jumbo.ProductCatalog.UI.Models;

public sealed record ImportResult(int Processed, IReadOnlyList<string> SkippedCodes);
