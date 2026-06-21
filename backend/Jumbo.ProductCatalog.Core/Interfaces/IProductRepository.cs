using Jumbo.ProductCatalog.Domain.Entities;

namespace Jumbo.ProductCatalog.Core.Interfaces;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Product?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task<Product?> GetByCodeIncludingArchivedAsync(string code, CancellationToken ct = default);
    Task<IReadOnlyList<Product>> GetByCodesIncludingArchivedAsync(IEnumerable<string> codes, CancellationToken ct = default);
    Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Product>> GetActiveAsync(CancellationToken ct = default);
    Task AddAsync(Product product, CancellationToken ct = default);
    Task UpdateAsync(Product product, CancellationToken ct = default);
    Task UpdateRangeAsync(IEnumerable<Product> products, CancellationToken ct = default);
    Task ImportBatchAsync(IEnumerable<Product> toAdd, IEnumerable<Product> toUpdate, CancellationToken ct = default);
    Task DeleteAsync(Product product, CancellationToken ct = default);
}
