using Jumbo.ProductCatalog.Core.DTOs;

namespace Jumbo.ProductCatalog.Core.Interfaces;

public interface IExportService
{
    Task<ExportResult> ExportActiveProductsAsync(CancellationToken ct = default);
}
