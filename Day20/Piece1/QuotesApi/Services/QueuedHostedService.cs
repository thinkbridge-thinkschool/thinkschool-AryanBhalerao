namespace QuotesApi.Services;

// A long-running BackgroundService that drains the work queue one item at a
// time. The host supplies `stoppingToken`, which it cancels the moment the app
// begins shutting down (Ctrl+C, SIGTERM, IHostApplicationLifetime.StopApplication).
// That single token does double duty:
//   1. it unblocks DequeueAsync so we stop waiting for new work, and
//   2. it is handed to each work item so in-flight work can wind down at a safe
//      point instead of being hard-killed.
// The host's StopAsync then waits (up to ShutdownTimeout, 30s by default) for
// ExecuteAsync to return — that wait is what makes shutdown "clean".
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
                // Shutdown requested while idle-waiting for work — leave the loop.
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
                // One faulty job must not tear down the processor — log and continue.
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
