using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
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

    private sealed class CommandHost(WebApplication app, Uri baseAddress) : IAsyncDisposable
    {
        public Uri BaseAddress { get; } = baseAddress;

        public static async Task<CommandHost> StartAsync()
        {
            var listener = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();

            var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = [], EnvironmentName = "Test" });
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
            app.MapGet("/purchase/business", (HttpContext h, CancellationToken ct) =>
                Rev869BPurchaseEndpoints.Run(PlainBusinessRejection, h, ct));
            app.MapGet("/opening-stock/business", (HttpContext h, CancellationToken ct) =>
                Rev869BPurchaseEndpoints.Run(PlainBusinessRejection, h, ct));

            await app.StartAsync();
            return new CommandHost(app, new Uri($"http://127.0.0.1:{port}"));
        }

        public async ValueTask DisposeAsync()
        {
            await app.StopAsync();
            await app.DisposeAsync();
        }
    }
}
