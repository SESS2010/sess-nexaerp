using SESS.NexaERP.Api.Security;
using SESS.NexaERP.Application.Authorization;
using SESS.NexaERP.Application.Stores;

namespace SESS.NexaERP.Api.Endpoints;

public static partial class VendorFinancialEvidenceEndpoints
{
    private static void MapBankAdviceEndpoints(RouteGroupBuilder group)
    {
        group.MapPost("/bank-advices", (HttpRequest request, HttpContext context,
            IVendorBankAdviceService service, CancellationToken ct) =>
            Run(async () =>
            {
                if (!request.Headers.TryGetValue("Idempotency-Key", out var keys) || keys.Count != 1)
                    throw new StoresValidationException("One Idempotency-Key header is required.");
                if (!request.HasFormContentType)
                    throw new StoresValidationException("Multipart form data with vendorId and one file is required.");
                var form = await request.ReadFormAsync(ct);
                if (form["vendorId"].Count != 1 || !Guid.TryParse(form["vendorId"].ToString(), out var vendor))
                    throw new StoresValidationException("One valid vendorId form field is required.");
                var file = form.Files.GetFile("file");
                if (form.Files.Count != 1 || file is null || file.Length is < 1 or > VendorBankAdviceLimits.MaximumBytes)
                    throw new StoresValidationException("One bank advice file of at most 5 MB is required.");
                await using var stream = file.OpenReadStream();
                using var buffer = new MemoryStream();
                await stream.CopyToAsync(buffer, ct);
                return await service.UploadAsync(new(vendor, file.FileName, file.ContentType,
                    buffer.ToArray(), keys.ToString()), ct);
            }, context, true))
            .DisableAntiforgery()
            .RequirePagePermission("accounts.vendor-financial-evidence", PagePermissionActions.UploadAttachment);

        group.MapGet("/bank-advices/{id:guid}", (Guid id, IVendorBankAdviceService service,
            HttpContext context, CancellationToken ct) => Run(() => service.GetAsync(id, ct), context, false))
            .RequirePagePermission("accounts.vendor-financial-evidence", PagePermissionActions.View);

        group.MapGet("/bank-advices/by-key", (string evidenceObjectKey, IVendorBankAdviceService service,
            HttpContext context, CancellationToken ct) => Run(() =>
            {
                const string prefix = "bank-advice:";
                if (!evidenceObjectKey.StartsWith(prefix, StringComparison.Ordinal) ||
                    !Guid.TryParseExact(evidenceObjectKey[prefix.Length..], "D", out var id))
                    throw new StoresValidationException("This reference is not a retained bank advice.");
                return service.GetAsync(id, ct);
            }, context, false))
            .RequirePagePermission("accounts.vendor-financial-evidence", PagePermissionActions.View);

        group.MapGet("/bank-advices/{id:guid}/content", DownloadBankAdvice)
            .RequirePagePermission("accounts.vendor-financial-evidence", PagePermissionActions.Download);
    }

    private static async Task<IResult> DownloadBankAdvice(Guid id, IVendorBankAdviceService service,
        HttpContext context, CancellationToken ct)
    {
        try
        {
            var file = await service.DownloadAsync(id, ct);
            return Results.File(file.Content, file.ContentType, file.FileName);
        }
        catch (KeyNotFoundException error) { return Results.NotFound(new { message = error.Message }); }
        catch (StoresConflictException error) { return Results.Conflict(new { message = error.Message }); }
        catch (UnauthorizedAccessException)
        {
            return context.User.Identity?.IsAuthenticated == true ? Results.Forbid() : Results.Unauthorized();
        }
    }
}
