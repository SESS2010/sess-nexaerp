using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;

namespace SESS.NexaERP.Api.Middleware;

public sealed record StandardErrorEnvelope(
    string Type,
    string Title,
    int Status,
    string Code,
    string Detail,
    string TraceId,
    IReadOnlyDictionary<string, string[]> Errors);

public sealed class StandardErrorEnvelopeMiddleware(
    RequestDelegate next,
    IOptions<JsonOptions> jsonOptions)
{
    private static readonly IReadOnlyDictionary<string, string[]> NoErrors =
        new Dictionary<string, string[]>(StringComparer.Ordinal);

    public async Task InvokeAsync(HttpContext context)
    {
        var originalBody = context.Response.Body;
        await using var responseBody = new ErrorCaptureStream(context, originalBody);
        context.Response.Body = responseBody;

        try
        {
            await next(context);

            if (context.Response.StatusCode < StatusCodes.Status400BadRequest)
            {
                return;
            }

            var legacy = await ReadLegacyErrorAsync(responseBody.ErrorBody, context.RequestAborted);
            var envelope = CreateEnvelope(
                context.Response.StatusCode,
                legacy.Detail,
                legacy.Errors,
                Activity.Current?.Id ?? context.TraceIdentifier);

            context.Response.Body = originalBody;
            context.Response.ContentLength = null;
            context.Response.ContentType = "application/problem+json; charset=utf-8";
            await JsonSerializer.SerializeAsync(
                originalBody,
                envelope,
                jsonOptions.Value.SerializerOptions,
                context.RequestAborted);
        }
        finally
        {
            context.Response.Body = originalBody;
        }
    }

    public static StandardErrorEnvelope CreateEnvelope(
        int status,
        string? existingDetail,
        IReadOnlyDictionary<string, string[]>? errors,
        string traceId)
    {
        var (slug, title, code, defaultDetail) = status switch
        {
            StatusCodes.Status400BadRequest => ("validation-error", "Validation failed", "VALIDATION_FAILED", "One or more fields are invalid."),
            StatusCodes.Status401Unauthorized => ("authentication-required", "Authentication required", "AUTHENTICATION_REQUIRED", "A valid OIDC bearer token and active employee identity are required."),
            StatusCodes.Status403Forbidden => ("permission-denied", "Permission denied", "PERMISSION_DENIED", "The current identity does not have permission for this operation in this company and scope."),
            StatusCodes.Status404NotFound => ("not-found", "Resource not found", "NOT_FOUND", "The requested resource was not found in the current scope."),
            StatusCodes.Status409Conflict => Conflict(existingDetail),
            _ when status >= StatusCodes.Status500InternalServerError => ("internal-error", "Unexpected server error", "INTERNAL_ERROR", "An unexpected error occurred."),
            _ => ("request-error", "Request failed", "REQUEST_FAILED", "The request could not be completed.")
        };

        var detail = status >= StatusCodes.Status500InternalServerError
            ? defaultDetail
            : string.IsNullOrWhiteSpace(existingDetail) ? defaultDetail : existingDetail.Trim();

        return new StandardErrorEnvelope(
            $"https://api.sess.example/problems/{slug}",
            title,
            status,
            code,
            detail,
            traceId,
            errors ?? NoErrors);
    }

    private static (string Slug, string Title, string Code, string Detail) Conflict(string? detail)
    {
        if (Contains(detail, "idempot"))
            return ("idempotency-conflict", "Idempotency conflict", "IDEMPOTENCY_CONFLICT", "The idempotency key was already used for a different request.");
        if (Contains(detail, "stale") || Contains(detail, "concurr") || Contains(detail, "version"))
            return ("concurrency-conflict", "Concurrency conflict", "CONCURRENCY_CONFLICT", "The record changed after it was loaded. Refresh and retry.");
        return ("business-rule-conflict", "Business rule conflict", "BUSINESS_RULE_CONFLICT", "The request conflicts with the current business state.");
    }

    private static bool Contains(string? value, string fragment) =>
        value?.Contains(fragment, StringComparison.OrdinalIgnoreCase) == true;

    private static async Task<(string? Detail, IReadOnlyDictionary<string, string[]>? Errors)> ReadLegacyErrorAsync(
        MemoryStream body,
        CancellationToken cancellationToken)
    {
        if (body.Length == 0) return (null, null);
        body.Position = 0;

        try
        {
            using var document = await JsonDocument.ParseAsync(body, cancellationToken: cancellationToken);
            if (document.RootElement.ValueKind != JsonValueKind.Object) return (null, null);

            var detail = ReadString(document.RootElement, "Detail")
                ?? ReadString(document.RootElement, "Message")
                ?? ReadString(document.RootElement, "Title");
            var errors = ReadErrors(document.RootElement);
            return (detail, errors);
        }
        catch (JsonException)
        {
            return (null, null);
        }
    }

    private static string? ReadString(JsonElement root, string name)
    {
        foreach (var property in root.EnumerateObject())
        {
            if (property.Name.Equals(name, StringComparison.OrdinalIgnoreCase)
                && property.Value.ValueKind == JsonValueKind.String)
                return property.Value.GetString();
        }
        return null;
    }

    private static IReadOnlyDictionary<string, string[]>? ReadErrors(JsonElement root)
    {
        var property = root.EnumerateObject().FirstOrDefault(candidate =>
            candidate.Name.Equals("Errors", StringComparison.OrdinalIgnoreCase));
        if (property.Value.ValueKind != JsonValueKind.Object) return null;

        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        foreach (var error in property.Value.EnumerateObject())
        {
            if (error.Value.ValueKind == JsonValueKind.Array)
            {
                errors[error.Name] = error.Value.EnumerateArray()
                    .Where(value => value.ValueKind == JsonValueKind.String)
                    .Select(value => value.GetString()!)
                    .ToArray();
            }
            else if (error.Value.ValueKind == JsonValueKind.String)
            {
                errors[error.Name] = [error.Value.GetString()!];
            }
        }
        return errors;
    }

    private sealed class ErrorCaptureStream(HttpContext context, Stream successBody) : Stream
    {
        public MemoryStream ErrorBody { get; } = new();

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        private Stream Current => context.Response.StatusCode >= StatusCodes.Status400BadRequest
            ? ErrorBody
            : successBody;

        public override void Flush() => Current.Flush();
        public override Task FlushAsync(CancellationToken cancellationToken) =>
            Current.FlushAsync(cancellationToken);
        public override void Write(byte[] buffer, int offset, int count) =>
            Current.Write(buffer, offset, count);
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            Current.WriteAsync(buffer, offset, count, cancellationToken);
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) =>
            Current.WriteAsync(buffer, cancellationToken);
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing) ErrorBody.Dispose();
            base.Dispose(disposing);
        }

        public override async ValueTask DisposeAsync()
        {
            await ErrorBody.DisposeAsync();
            GC.SuppressFinalize(this);
        }
    }
}
