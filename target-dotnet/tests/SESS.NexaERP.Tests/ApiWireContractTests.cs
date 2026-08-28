using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using SESS.NexaERP.Api.Middleware;
using SESS.NexaERP.Api.Serialization;

namespace SESS.NexaERP.Tests;

public sealed class ApiWireContractTests
{
    private static readonly string[] EnvelopeProperties =
        ["Code", "Detail", "Errors", "Status", "Title", "TraceId", "Type"];

    [Fact]
    public async Task Global_http_json_contract_serializes_dtos_and_anonymous_objects_as_PascalCase()
    {
        await using var host = await ContractHost.StartAsync();
        using var client = new HttpClient { BaseAddress = host.BaseAddress };

        using var dto = JsonDocument.Parse(await client.GetStringAsync("/dto"));
        Assert.Equal(
            ["EmployeeId", "EmployeeName"],
            dto.RootElement.EnumerateObject().Select(property => property.Name).Order().ToArray());

        using var anonymous = JsonDocument.Parse(await client.GetStringAsync("/anonymous"));
        Assert.Equal(
            ["App", "SourceSystem"],
            anonymous.RootElement.EnumerateObject().Select(property => property.Name).Order().ToArray());
    }

    [Theory]
    [InlineData("/validation", HttpStatusCode.BadRequest, "VALIDATION_FAILED", "validation-error")]
    [InlineData("/unauthorized", HttpStatusCode.Unauthorized, "AUTHENTICATION_REQUIRED", "authentication-required")]
    [InlineData("/forbidden", HttpStatusCode.Forbidden, "PERMISSION_DENIED", "permission-denied")]
    [InlineData("/missing", HttpStatusCode.NotFound, "NOT_FOUND", "not-found")]
    [InlineData("/not-mapped", HttpStatusCode.NotFound, "NOT_FOUND", "not-found")]
    [InlineData("/stale", HttpStatusCode.Conflict, "CONCURRENCY_CONFLICT", "concurrency-conflict")]
    [InlineData("/idempotency", HttpStatusCode.Conflict, "IDEMPOTENCY_CONFLICT", "idempotency-conflict")]
    [InlineData("/business", HttpStatusCode.Conflict, "BUSINESS_RULE_CONFLICT", "business-rule-conflict")]
    public async Task Every_handled_failure_uses_the_exact_standard_envelope(
        string path,
        HttpStatusCode status,
        string code,
        string typeSlug)
    {
        await using var host = await ContractHost.StartAsync();
        using var client = new HttpClient { BaseAddress = host.BaseAddress };

        var response = await client.GetAsync(path);

        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(
            EnvelopeProperties,
            document.RootElement.EnumerateObject().Select(property => property.Name).Order().ToArray());
        Assert.Equal((int)status, document.RootElement.GetProperty("Status").GetInt32());
        Assert.Equal(code, document.RootElement.GetProperty("Code").GetString());
        Assert.Equal($"https://api.sess.example/problems/{typeSlug}", document.RootElement.GetProperty("Type").GetString());
        Assert.False(string.IsNullOrWhiteSpace(document.RootElement.GetProperty("Title").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(document.RootElement.GetProperty("Detail").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(document.RootElement.GetProperty("TraceId").GetString()));
        Assert.Equal(JsonValueKind.Object, document.RootElement.GetProperty("Errors").ValueKind);
        Assert.False(document.RootElement.TryGetProperty("message", out _));
    }

    [Fact]
    public async Task Unhandled_failure_uses_internal_error_without_exposing_exception_text()
    {
        await using var host = await ContractHost.StartAsync();
        using var client = new HttpClient { BaseAddress = host.BaseAddress };

        var response = await client.GetAsync("/exception");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.DoesNotContain("secret database detail", body, StringComparison.Ordinal);
        using var document = JsonDocument.Parse(body);
        Assert.Equal("INTERNAL_ERROR", document.RootElement.GetProperty("Code").GetString());
        Assert.Equal("An unexpected error occurred.", document.RootElement.GetProperty("Detail").GetString());
    }

    [Fact]
    public async Task Validation_errors_are_preserved_inside_the_standard_Errors_object()
    {
        await using var host = await ContractHost.StartAsync();
        using var client = new HttpClient { BaseAddress = host.BaseAddress };

        var response = await client.GetAsync("/field-validation");
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        var errors = document.RootElement.GetProperty("Errors");
        Assert.Equal("RequiredByDate is required.", errors.GetProperty("RequiredByDate")[0].GetString());
    }

    [Fact]
    public async Task Framework_method_not_allowed_failure_uses_the_generic_standard_envelope()
    {
        await using var host = await ContractHost.StartAsync();
        using var client = new HttpClient { BaseAddress = host.BaseAddress };

        var response = await client.PostAsync("/dto", null);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
        Assert.Equal("REQUEST_FAILED", document.RootElement.GetProperty("Code").GetString());
        Assert.Equal(
            "https://api.sess.example/problems/request-error",
            document.RootElement.GetProperty("Type").GetString());
        Assert.Equal(
            EnvelopeProperties,
            document.RootElement.EnumerateObject().Select(property => property.Name).Order().ToArray());
    }

    private sealed record ContractProbe(Guid EmployeeId, string EmployeeName);

    private sealed class ContractHost(WebApplication app, Uri baseAddress) : IAsyncDisposable
    {
        public Uri BaseAddress { get; } = baseAddress;

        public static async Task<ContractHost> StartAsync()
        {
            var listener = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();

            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                Args = [],
                EnvironmentName = "Test"
            });
            builder.WebHost.UseUrls($"http://127.0.0.1:{port}");
            builder.Services.ConfigureHttpJsonOptions(options => ApiJsonContract.Configure(options.SerializerOptions));

            var app = builder.Build();
            app.UseMiddleware<StandardErrorEnvelopeMiddleware>();
            app.UseMiddleware<ExceptionHandlingMiddleware>();
            app.MapGet("/dto", () => Results.Ok(new ContractProbe(Guid.Parse("145e2c65-3f72-4ef3-b7d0-9f323404298c"), "Priya E")));
            app.MapGet("/anonymous", () => Results.Ok(new { app = "SESS NexaERP", sourceSystem = "advance" }));
            app.MapGet("/validation", () => Results.BadRequest(new { message = "RequiredByDate is required." }));
            app.MapGet("/field-validation", () => Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["RequiredByDate"] = ["RequiredByDate is required."]
            }));
            app.MapGet("/unauthorized", () => Results.Unauthorized());
            app.MapGet("/forbidden", () => Results.StatusCode(StatusCodes.Status403Forbidden));
            app.MapGet("/missing", () => Results.NotFound());
            app.MapGet("/stale", () => Results.Conflict(new { message = "Stale record version. Refresh and retry." }));
            app.MapGet("/idempotency", () => Results.Conflict(new { message = "Idempotency key conflicts with a different request." }));
            app.MapGet("/business", () => Results.Conflict(new { message = "Document is already finalized." }));
            app.MapGet("/exception", IResult () => throw new InvalidOperationException("secret database detail"));

            await app.StartAsync();
            return new ContractHost(app, new Uri($"http://127.0.0.1:{port}"));
        }

        public async ValueTask DisposeAsync()
        {
            await app.StopAsync();
            await app.DisposeAsync();
        }
    }
}
