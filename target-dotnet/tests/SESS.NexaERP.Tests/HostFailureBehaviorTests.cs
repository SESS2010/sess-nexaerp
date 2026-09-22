using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SESS.NexaERP.Api;
using SESS.NexaERP.Application.Stores;

namespace SESS.NexaERP.Tests;

public sealed class HostFailureBehaviorTests
{
#if HOST_FAILURE_WITNESS
    [Fact]
    public async Task NotificationFailureRetriesAndPortCollisionDoesNotStopExistingHost()
    {
        var processor = new FailingOnceProcessor();
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Development" });
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddSingleton<INotificationDueEventProcessor>(processor);
        builder.Services.AddHostedService<InAppNotificationWorker>();
        await using var first = builder.Build();
        first.MapGet("/health/live",() => Results.Ok("alive"));
        Assert.Equal(BackgroundServiceExceptionBehavior.StopHost,
            first.Services.GetRequiredService<IOptions<HostOptions>>().Value.BackgroundServiceExceptionBehavior);
        await first.StartAsync();
        await processor.FirstAttempt.Task.WaitAsync(TimeSpan.FromSeconds(10));
        var address = Assert.Single(first.Urls);
        using var client = new HttpClient { BaseAddress = new Uri(address) };
        Assert.Equal(HttpStatusCode.OK,(await client.GetAsync("/health/live")).StatusCode);

        var other = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Development" });
        other.Logging.ClearProviders();
        other.WebHost.UseUrls(address);
        await using var second = other.Build();
        second.MapGet("/health/live",() => Results.Ok("second"));
        var bindFailure = await Record.ExceptionAsync(() => second.StartAsync());
        Assert.IsType<IOException>(bindFailure);
        Assert.Contains("address already in use",bindFailure!.ToString(),StringComparison.OrdinalIgnoreCase);
        Assert.False(first.Lifetime.ApplicationStopping.IsCancellationRequested);
        Assert.Equal(HttpStatusCode.OK,(await client.GetAsync("/health/live")).StatusCode);

        // Exercise the worker's actual one-minute interval; no production timer
        // override or relaxed BackgroundServiceExceptionBehavior.
        await processor.Retry.Task.WaitAsync(TimeSpan.FromSeconds(75));
        Assert.Equal(2,processor.Attempts);
        Assert.False(first.Lifetime.ApplicationStopping.IsCancellationRequested);
        Assert.Equal(HttpStatusCode.OK,(await client.GetAsync("/health/live")).StatusCode);
        await first.StopAsync();
    }

#endif
    private sealed class FailingOnceProcessor : INotificationDueEventProcessor
    {
        public TaskCompletionSource FirstAttempt { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Retry { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int Attempts;
        public Task<int> RefreshAsync(DateTimeOffset now,CancellationToken ct)
        {
            if (Interlocked.Increment(ref Attempts) == 1)
            {
                FirstAttempt.TrySetResult();
                throw new IOException("Injected notification database connection loss.");
            }
            Retry.TrySetResult();
            return Task.FromResult(0);
        }
    }
}
