namespace Jumbo.ProductCatalog.Core.DTOs;

public sealed record ImportResult(int Processed, IReadOnlyList<string> SkippedCodes);
