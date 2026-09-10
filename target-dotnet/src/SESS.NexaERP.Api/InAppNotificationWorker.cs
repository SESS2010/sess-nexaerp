using SESS.NexaERP.Application.Stores;

namespace SESS.NexaERP.Api;

public sealed class InAppNotificationWorker(
    IServiceScopeFactory scopes,
    ILogger<InAppNotificationWorker> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RunOnce(stoppingToken);
        using var timer = new PeriodicTimer(Interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
            await RunOnce(stoppingToken);
    }

    private async Task RunOnce(CancellationToken ct)
    {
        try
        {
            await using var scope = scopes.CreateAsyncScope();
            var processor = scope.ServiceProvider.GetRequiredService<INotificationDueEventProcessor>();
            var changed = await processor.RefreshAsync(DateTimeOffset.UtcNow, ct);
            if (changed > 0) logger.LogInformation("Notification refresh changed {Count} events.", changed);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "In-app notification refresh failed; the next scheduled pass will retry.");
        }
    }
}
