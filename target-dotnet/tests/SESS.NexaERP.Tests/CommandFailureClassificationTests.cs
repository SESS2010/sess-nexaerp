using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SESS.NexaERP.Api.Endpoints;
using SESS.NexaERP.Api.Middleware;
using SESS.NexaERP.Api.Security;
using SESS.NexaERP.Api.Serialization;
using SESS.NexaERP.Application.Audit;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Purchase;

namespace SESS.NexaERP.Tests;

/// <summary>
/// The shared Rev869B command wrapper is used by 75 endpoint registrations across Purchase and
/// Stores, INCLUDING the opening-stock ceremony commands. Those ceremonies are posted once, on
/// 5-6 October. If an infrastructure failure there is reported as "you filled the form wrongly",
/// the real cause is never learned and the ceremony cannot be re-run for a second look.
///
/// These tests pin the boundary: an infrastructure failure is a 500 carrying a TraceId and never
/// a validation error, while a deliberate business rejection keeps its 400.
/// </summary>
public sealed class CommandFailureClassificationTests
{
    // A transaction scope whose disposal fails, exactly as Npgsql does when the transaction has
    // completed or its connection has been lost. C# lets a failure in DisposeAsync REPLACE the
    // exception from the block, which is how the real cause used to be destroyed.
    private sealed class ScopeThatFailsOnDispose : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => throw new ObjectDisposedException("NpgsqlTransaction");
    }

    private static async Task<Rev869BDocumentResult> BusinessRejectionUnderFailedDisposal()
    {
        await using var scope = new ScopeThatFailsOnDispose();
        // A real, correct business rejection. Its message must not be what decides the status.
        throw new InvalidOperationException("RFQ split exceeds approved handoff quantity.");
    }

    // A scope disposing the way Rev869BTransactionScope now does: its own rollback fails, and
    // that failure is suppressed so it cannot replace the exception from the block.
    private sealed class ScopeThatSuppressesItsDisposalFailure : IAsyncDisposable
    {
        public ValueTask DisposeAsync()
        {
            try { throw new ObjectDisposedException(null, "NpgsqlTransaction"); }
            catch (ObjectDisposedException) { /* recorded on the activity, never rethrown */ }
            return ValueTask.CompletedTask;
        }
    }

    // The actual RFQ failure of 23 September, in the shape it really reaches Run(): the client
    // sent QuoteDueAt with +05:30, Npgsql refused it with ArgumentException during SaveChanges,
    // and EF wrapped that in DbUpdateException. The disposal rollback that followed used to
    // replace it with ObjectDisposedException("NpgsqlTransaction"), classified as a 400. The
    // real-request witness is the LOW band of the complete purchase flow.
    private static async Task<Rev869BDocumentResult> NonUtcOffsetUnderSuppressedDisposal()
    {
        await using var scope = new ScopeThatSuppressesItsDisposalFailure();
        throw new Microsoft.EntityFrameworkCore.DbUpdateException(
            "An error occurred while saving the entity changes. See the inner exception for details.",
            new ArgumentException(
                "Cannot write DateTimeOffset with Offset=05:30:00 to PostgreSQL type 'timestamp with time zone', " +
                "only offset 0 (UTC) is supported.", "value"));
    }

    [Fact]
    public async Task The_real_rfq_cause_survives_the_disposal_that_used_to_replace_it()
    {
        await using var host = await CommandHost.StartAsync();
        using var client = new HttpClient { BaseAddress = host.BaseAddress };

        var response = await client.GetAsync("/purchase/non-utc-offset");
        var body = await response.Content.ReadAsStringAsync();

        // Today this is a 500, not a 400: DbUpdateException is not a business rejection in Run().
        // The operator gets a TraceId, never the name of a disposed object.
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        using var document = JsonDocument.Parse(body);
        Assert.Equal("INTERNAL_ERROR", document.RootElement.GetProperty("Code").GetString());
        Assert.DoesNotContain("NpgsqlTransaction", body, StringComparison.Ordinal);

        // What 7003c02 bought: the real cause reaches the server log instead of being destroyed.
        Assert.Contains(host.LoggedExceptions, x => x.Contains("only offset 0 (UTC) is supported", StringComparison.Ordinal));
        Assert.DoesNotContain(host.LoggedExceptions, x => x.Contains("ObjectDisposedException", StringComparison.Ordinal));
    }

    private static async Task<Rev869BDocumentResult> PlainBusinessRejection()
    {
        await Task.Yield();
        throw new InvalidOperationException("RFQ split exceeds approved handoff quantity.");
    }

    private static async Task<Rev869BDocumentResult> InfrastructureFailure()
    {
        await Task.Yield();
        throw new ObjectDisposedException("NpgsqlTransaction");
    }

    [Theory]
    // The purchase command and the opening-stock ceremony command share one wrapper, so both are
    // pinned. If the mechanism ever bites during a ceremony we need the real cause in the trace.
    [InlineData("/purchase/masked-disposal")]
    [InlineData("/opening-stock/masked-disposal")]
    [InlineData("/purchase/infrastructure")]
    [InlineData("/opening-stock/infrastructure")]
    public async Task Infrastructure_failure_is_never_reported_as_a_validation_error(string path)
    {
        await using var host = await CommandHost.StartAsync();
        using var client = new HttpClient { BaseAddress = host.BaseAddress };

        var response = await client.GetAsync(path);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        using var document = JsonDocument.Parse(body);
        Assert.Equal("INTERNAL_ERROR", document.RootElement.GetProperty("Code").GetString());
        Assert.NotEqual("VALIDATION_FAILED", document.RootElement.GetProperty("Code").GetString());

        // The cause must be recoverable through the trace, not published to the operator.
        Assert.False(string.IsNullOrWhiteSpace(document.RootElement.GetProperty("TraceId").GetString()));
        Assert.DoesNotContain("NpgsqlTransaction", body, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("/purchase/business")]
    [InlineData("/opening-stock/business")]
    public async Task Deliberate_business_rejection_keeps_its_validation_status(string path)
    {
        await using var host = await CommandHost.StartAsync();
        using var client = new HttpClient { BaseAddress = host.BaseAddress };

        var response = await client.GetAsync(path);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var document = JsonDocument.Parse(body);
        Assert.Equal("VALIDATION_FAILED", document.RootElement.GetProperty("Code").GetString());
        // The operator is told the actual business reason, which is the point of a 400.
        Assert.Equal("RFQ split exceeds approved handoff quantity.",
            document.RootElement.GetProperty("Detail").GetString());
    }

    private sealed class NoOpAuditWriter : IAuditWriter
    {
        public Task WriteAsync(string module, string action, string entityName, string entityId,
            object? before, object? after, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    // Keeps what the central handler logs, so a test can prove where the real cause went.
    private sealed class ExceptionCapture : ILoggerProvider, ILogger
    {
        public System.Collections.Concurrent.ConcurrentQueue<string> Exceptions { get; } = new();
        public ILogger CreateLogger(string categoryName) => this;
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (exception is not null) Exceptions.Enqueue(exception.ToString());
        }
        public void Dispose() { }
    }

    private sealed class CommandHost(WebApplication app, Uri baseAddress, ExceptionCapture capture) : IAsyncDisposable
    {
        public Uri BaseAddress { get; } = baseAddress;
        public IReadOnlyCollection<string> LoggedExceptions => capture.Exceptions;

        public static async Task<CommandHost> StartAsync()
        {
            var listener = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();

            var capture = new ExceptionCapture();
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = [], EnvironmentName = "Test" });
            builder.Logging.AddProvider(capture);
            builder.WebHost.UseUrls($"http://127.0.0.1:{port}");
            builder.Services.ConfigureHttpJsonOptions(options => ApiJsonContract.Configure(options.SerializerOptions));
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddScoped<ICurrentUser, ClaimsCurrentUser>();
            builder.Services.AddScoped<IAuditWriter, NoOpAuditWriter>();

            var app = builder.Build();
            app.UseMiddleware<StandardErrorEnvelopeMiddleware>();
            app.UseMiddleware<ExceptionHandlingMiddleware>();

            // The real shared wrapper, not a copy of its logic.
            app.MapGet("/purchase/masked-disposal", (HttpContext h, CancellationToken ct) =>
                Rev869BPurchaseEndpoints.Run(BusinessRejectionUnderFailedDisposal, h, ct));
            app.MapGet("/opening-stock/masked-disposal", (HttpContext h, CancellationToken ct) =>
                Rev869BPurchaseEndpoints.Run(BusinessRejectionUnderFailedDisposal, h, ct));
            app.MapGet("/purchase/infrastructure", (HttpContext h, CancellationToken ct) =>
                Rev869BPurchaseEndpoints.Run(InfrastructureFailure, h, ct));
            app.MapGet("/opening-stock/infrastructure", (HttpContext h, CancellationToken ct) =>
                Rev869BPurchaseEndpoints.Run(InfrastructureFailure, h, ct));
            app.MapGet("/purchase/non-utc-offset", (HttpContext h, CancellationToken ct) =>
                Rev869BPurchaseEndpoints.Run(NonUtcOffsetUnderSuppressedDisposal, h, ct));
            app.MapGet("/purchase/business", (HttpContext h, CancellationToken ct) =>
                Rev869BPurchaseEndpoints.Run(PlainBusinessRejection, h, ct));
            app.MapGet("/opening-stock/business", (HttpContext h, CancellationToken ct) =>
                Rev869BPurchaseEndpoints.Run(PlainBusinessRejection, h, ct));

            await app.StartAsync();
            return new CommandHost(app, new Uri($"http://127.0.0.1:{port}"), capture);
        }

        public async ValueTask DisposeAsync()
        {
            await app.StopAsync();
            await app.DisposeAsync();
        }
    }
}
