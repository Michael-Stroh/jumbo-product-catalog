using Jumbo.ProductCatalog.Core.Interfaces;
using Jumbo.ProductCatalog.Domain.Configs;
using Microsoft.Extensions.Options;

namespace Jumbo.ProductCatalog.Worker;

public partial class Worker(
    IServiceScopeFactory scopeFactory,
    IOptions<ExportConfig> exportConfig,
    ILogger<Worker> logger) : BackgroundService
{
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(exportConfig.Value.IntervalMinutes);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Run once immediately on startup, then on the configured interval.
        while (!stoppingToken.IsCancellationRequested)
        {
            await RunExportAsync(stoppingToken);
            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task RunExportAsync(CancellationToken ct)
    {
        LogExportCycleStarted();
        using var scope = scopeFactory.CreateScope();
        var exportService = scope.ServiceProvider.GetRequiredService<IExportService>();
        var result = await exportService.ExportActiveProductsAsync(ct);
        LogExportCycleCompleted(result.ExportedCount, result.FileName);
    }

    [LoggerMessage(LogLevel.Information, "Export cycle started")]
    private partial void LogExportCycleStarted();

    [LoggerMessage(LogLevel.Information, "Export cycle completed: {ExportedCount} products exported to {FileName}")]
    private partial void LogExportCycleCompleted(int exportedCount, string fileName);
}

