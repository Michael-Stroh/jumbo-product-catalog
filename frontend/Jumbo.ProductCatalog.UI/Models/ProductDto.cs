namespace Jumbo.ProductCatalog.UI.Models;

public sealed record ProductDto(
    Guid Id,
    string Code,
    string Name,
    int Category,
    string? Content,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt);
