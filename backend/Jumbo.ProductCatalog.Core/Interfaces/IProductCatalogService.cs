using Jumbo.ProductCatalog.Core.DTOs;
using Jumbo.ProductCatalog.Domain.Common;

namespace Jumbo.ProductCatalog.Core.Interfaces;

public interface IProductCatalogService
{
    Task<ProductDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<ProductDto>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ProductDto>> GetActiveAsync(CancellationToken ct = default);
    Task<Result<ProductDto>> CreateAsync(CreateProductRequest request, CancellationToken ct = default);
    Task<Result<ProductDto>> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<ImportResult> ImportAsync(IReadOnlyList<CreateProductRequest> items, CancellationToken ct = default);
}
