namespace QuotesApi.Services;

// Drains the background work queue
public sealed class QueuedHostedService : BackgroundService
{
    private readonly IBackgroundTaskQueue _queue;
    private readonly ILogger<QueuedHostedService> _logger;

    public QueuedHostedService(IBackgroundTaskQueue queue, ILogger<QueuedHostedService> logger)
    {
        _queue = queue;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Queue processor started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            Func<CancellationToken, ValueTask> workItem;
            try
            {
                workItem = await _queue.DequeueAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            try
            {
                await workItem(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Work item canceled cooperatively during shutdown.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Background work item threw.");
            }
        }

        _logger.LogInformation(
            "Queue processor stopped. {Remaining} item(s) left undrained.", _queue.Count);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stop signal received; finishing in-flight work…");
        await base.StopAsync(cancellationToken);
        _logger.LogInformation("Queue processor shut down cleanly.");
    }
}
