using Jumbo.ProductCatalog.Core.Interfaces;
using Jumbo.ProductCatalog.Domain.Entities;
using Jumbo.ProductCatalog.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jumbo.ProductCatalog.Infrastructure.Repositories;

public sealed class ProductRepository(ProductDbContext context) : IProductRepository
{
    public Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        context.Products.FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<Product?> GetByCodeAsync(string code, CancellationToken ct = default) =>
        context.Products.FirstOrDefaultAsync(p => p.Code == code, ct);

    // Bypasses the global IsArchived filter — used to detect archived products for reactivation.
    public Task<Product?> GetByCodeIncludingArchivedAsync(string code, CancellationToken ct = default) =>
        context.Products.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Code == code, ct);

    // Bulk variant — fetches a set of codes in one WHERE IN query for import.
    public async Task<IReadOnlyList<Product>> GetByCodesIncludingArchivedAsync(IEnumerable<string> codes, CancellationToken ct = default)
    {
        var codeSet = codes.ToHashSet();
        return await context.Products.IgnoreQueryFilters().Where(p => codeSet.Contains(p.Code)).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken ct = default) =>
        await context.Products.ToListAsync(ct);

    public async Task<IReadOnlyList<Product>> GetActiveAsync(CancellationToken ct = default) =>
        await context.Products.Where(p => p.IsActive).ToListAsync(ct);

    public async Task AddAsync(Product product, CancellationToken ct = default)
    {
        await context.Products.AddAsync(product, ct);
        await context.SaveChangesAsync(ct);
    }

    public Task UpdateAsync(Product product, CancellationToken ct = default)
    {
        context.Update(product);
        return context.SaveChangesAsync(ct);
    }

    public Task UpdateRangeAsync(IEnumerable<Product> products, CancellationToken ct = default)
    {
        context.UpdateRange(products);
        return context.SaveChangesAsync(ct);
    }

    // Single SaveChangesAsync wraps both inserts and updates in one implicit transaction.
    public async Task ImportBatchAsync(IEnumerable<Product> toAdd, IEnumerable<Product> toUpdate, CancellationToken ct = default)
    {
        await context.Products.AddRangeAsync(toAdd, ct);
        context.Products.UpdateRange(toUpdate);
        await context.SaveChangesAsync(ct);
    }

    // soft delete — sets IsArchived=true; global query filter keeps archived rows invisible.
    public Task DeleteAsync(Product product, CancellationToken ct = default)
    {
        product.IsArchived = true;
        context.Update(product);
        return context.SaveChangesAsync(ct);
    }
}
