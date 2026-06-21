namespace Jumbo.ProductService.Worker;

public partial class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;

    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            LogWorkerRunning(DateTimeOffset.Now);
            await Task.Delay(1000, stoppingToken);
        }
    }

    [LoggerMessage(LogLevel.Information, "Worker running at: {Timestamp}")]
    private partial void LogWorkerRunning(DateTimeOffset timestamp);
}
