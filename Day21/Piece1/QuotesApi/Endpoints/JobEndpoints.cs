using QuotesApi.Services;

namespace QuotesApi.Endpoints;

public static class JobEndpoints
{
    // Enqueue slow demo jobs
    public static IEndpointRouteBuilder MapJobEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/jobs/slow", async (
            int? count,
            int? seconds,
            IBackgroundTaskQueue queue,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("QuotesApi.Jobs");
            var jobs = Math.Clamp(count ?? 1, 1, 50);
            var duration = Math.Clamp(seconds ?? 5, 1, 60);

            for (var i = 1; i <= jobs; i++)
            {
                var jobId = i;
                await queue.QueueAsync(async ct =>
                {
                    logger.LogInformation("Job {JobId} started ({Seconds}s of work).", jobId, duration);
                    for (var elapsed = 1; elapsed <= duration; elapsed++)
                    {
                        // Cooperative cancellation point
                        await Task.Delay(TimeSpan.FromSeconds(1), ct);
                        logger.LogInformation("Job {JobId} progress {Elapsed}/{Seconds}s.", jobId, elapsed, duration);
                    }
                    logger.LogInformation("Job {JobId} completed.", jobId);
                });
            }

            return Results.Accepted(value: new { enqueued = jobs, secondsEach = duration });
        });

        return app;
    }
}
