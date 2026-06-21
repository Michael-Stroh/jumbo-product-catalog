using System.Text.Json;
using Jumbo.ProductCatalog.Core.DTOs;
using Jumbo.ProductCatalog.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Jumbo.ProductCatalog.Core.Services;

public sealed partial class ExportService(
    IProductRepository repository,
    IFileStorageService storageWriter,
    ILogger<ExportService> logger) : IExportService
{
    private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public async Task<ExportResult> ExportActiveProductsAsync(CancellationToken ct = default)
    {
        var products = await repository.GetActiveAsync(ct);
        var fileName = $"active-products-{DateTime.UtcNow:yyyyMMddHHmmss}.json";

        LogExportStarted(products.Count);

        var data = JsonSerializer.SerializeToUtf8Bytes(
            products.Select(p => new
            {
                p.Id,
                p.Code,
                p.Name,
                Category = p.Category.ToString(),
                p.Content,
                p.IsActive,
                p.CreatedAt,
                p.UpdatedAt,
            }),
            _jsonOptions);

        await storageWriter.WriteAsync(fileName, data, ct);

        LogExportCompleted(products.Count, fileName);
        return new ExportResult(products.Count, fileName);
    }

    [LoggerMessage(LogLevel.Information, "Export started: queried {Count} active products")]
    private partial void LogExportStarted(int count);

    [LoggerMessage(LogLevel.Information, "Export completed: {Count} products written to {FileName}")]
    private partial void LogExportCompleted(int count, string fileName);
}
