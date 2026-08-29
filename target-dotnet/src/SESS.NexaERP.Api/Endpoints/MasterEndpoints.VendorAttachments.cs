using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Api.Security;
using SESS.NexaERP.Application.Audit;
using SESS.NexaERP.Application.Authorization;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Domain.Masters;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Api.Endpoints;

public static partial class MasterEndpoints
{
    private const long MaxVendorAttachmentBytes = 5 * 1024 * 1024;
    private static readonly string[] AllowedVendorAttachmentContentTypes =
        ["application/pdf", "image/jpeg", "image/png"];

    private static void MapVendorAttachmentEndpoints(RouteGroupBuilder group)
    {
        // Next free vendor code, continuing the dominant existing series and
        // skipping existing records. The imported legacy master uses
        // SESS-V-#### ; that series wins whenever it is present, with VEN-###
        // as the fallback for an empty database.
        group.MapGet("/vendors/next-code", async (NexaErpDbContext db, CancellationToken cancellationToken) =>
        {
            static int HighestSuffix(IEnumerable<string> codes, string prefix) => codes
                .Where(code => code.StartsWith(prefix))
                .Select(code => int.TryParse(code[prefix.Length..], out var number) ? number : 0)
                .DefaultIfEmpty(0)
                .Max();

            var codes = await db.Vendors.AsNoTracking()
                .Where(vendor => vendor.VendorCode.StartsWith("SESS-V-") || vendor.VendorCode.StartsWith("VEN-"))
                .Select(vendor => vendor.VendorCode)
                .ToListAsync(cancellationToken);
            var highestLegacy = HighestSuffix(codes, "SESS-V-");
            var nextCode = highestLegacy > 0
                ? $"SESS-V-{highestLegacy + 1:D4}"
                : $"VEN-{HighestSuffix(codes, "VEN-") + 1:D3}";
            return Results.Ok(new { VendorCode = nextCode });
        }).RequirePagePermission("masters.vendors", PagePermissionActions.Create);

        group.MapPost("/vendors/attachments", async (HttpRequest request, NexaErpDbContext db, IAuditWriter audit, ICurrentUser currentUser, CancellationToken cancellationToken) =>
        {
            if (!request.HasFormContentType)
            {
                return Results.BadRequest(new { message = "Multipart form data with a 'file' and a 'kind' field is required." });
            }

            var form = await request.ReadFormAsync(cancellationToken);
            var file = form.Files.GetFile("file");
            var kind = form["kind"].ToString().Trim().ToUpperInvariant();
            if (file is null || file.Length == 0)
            {
                return Results.BadRequest(new { message = "An attachment file is required." });
            }
            if (!VendorAttachmentKinds.IsValid(kind))
            {
                return Results.BadRequest(new { message = $"Attachment kind must be {VendorAttachmentKinds.BankLeaf}, {VendorAttachmentKinds.GstCertificate} or {VendorAttachmentKinds.PanCard}." });
            }
            if (file.Length > MaxVendorAttachmentBytes)
            {
                return Results.BadRequest(new { message = "Attachment exceeds the 5 MB limit." });
            }
            if (!AllowedVendorAttachmentContentTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
            {
                return Results.BadRequest(new { message = "Only PDF, JPEG or PNG attachments are accepted." });
            }

            await using var stream = file.OpenReadStream();
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer, cancellationToken);

            var attachment = new VendorAttachment
            {
                Kind = kind,
                FileName = Path.GetFileName(file.FileName),
                ContentType = file.ContentType,
                SizeBytes = file.Length,
                Content = buffer.ToArray(),
                CreatedBy = currentUser.LoginId
            };
            db.VendorAttachments.Add(attachment);
            await db.SaveChangesAsync(cancellationToken);
            await audit.WriteAsync("Masters", "UploadAttachment", nameof(VendorAttachment), attachment.Id.ToString(), null,
                new { attachment.Kind, attachment.FileName, attachment.SizeBytes }, cancellationToken);

            return Results.Created($"/api/v1/masters/vendors/attachments/{attachment.Id}",
                new { attachment.Id, attachment.Kind, attachment.FileName, attachment.ContentType, attachment.SizeBytes });
        })
        .DisableAntiforgery()
        .RequirePagePermission("masters.vendors", PagePermissionActions.UploadAttachment);

        group.MapGet("/vendors/attachments/{attachmentId:guid}", async (Guid attachmentId, NexaErpDbContext db, CancellationToken cancellationToken) =>
        {
            var attachment = await db.VendorAttachments.AsNoTracking()
                .SingleOrDefaultAsync(existing => existing.Id == attachmentId, cancellationToken);
            return attachment is null
                ? Results.NotFound(new { message = "Attachment not found." })
                : Results.File(attachment.Content, attachment.ContentType, attachment.FileName);
        }).RequirePagePermission("masters.vendors", PagePermissionActions.Download);
    }

    /// <summary>
    /// Bank details ride in BankMetadataJson as
    /// {"bankName","accountHolder","accountNumber","ifsc","branch"}.
    /// All fields are optional, but a supplied IFSC must be a valid Indian
    /// IFSC and a supplied account number must be 6-18 digits.
    /// Returns the failure message, or null when valid.
    /// </summary>
    private static string? ValidateVendorBankMetadata(string? bankMetadataJson)
    {
        if (string.IsNullOrWhiteSpace(bankMetadataJson))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(bankMetadataJson);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return "Bank metadata must be a JSON object.";
            }

            if (root.TryGetProperty("ifsc", out var ifscProperty))
            {
                var ifsc = ifscProperty.GetString()?.Trim().ToUpperInvariant();
                if (!string.IsNullOrEmpty(ifsc) && !System.Text.RegularExpressions.Regex.IsMatch(ifsc, "^[A-Z]{4}0[A-Z0-9]{6}$"))
                {
                    return "Invalid IFSC code format (expected e.g. HDFC0001234).";
                }
            }

            if (root.TryGetProperty("accountNumber", out var accountProperty))
            {
                var account = accountProperty.GetString()?.Trim();
                if (!string.IsNullOrEmpty(account) && !System.Text.RegularExpressions.Regex.IsMatch(account, "^[0-9]{6,18}$"))
                {
                    return "Bank account number must be 6-18 digits.";
                }
            }

            return null;
        }
        catch (JsonException)
        {
            return "Bank metadata is not valid JSON.";
        }
    }

    /// <summary>
    /// The vendor's AttachmentMetadataJson must reference an uploaded GST
    /// certificate: {"gstCertificate":{"id":"..."},"bankLeaf":{...}?}.
    /// Returns the parse/lookup failure message, or null when valid.
    /// </summary>
    private static async Task<string?> ValidateVendorGstCertificateAsync(string? attachmentMetadataJson, NexaErpDbContext db, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(attachmentMetadataJson))
        {
            return "GST certificate attachment is required.";
        }

        try
        {
            using var document = JsonDocument.Parse(attachmentMetadataJson);
            if (!document.RootElement.TryGetProperty("gstCertificate", out var certificate)
                || !certificate.TryGetProperty("id", out var idProperty)
                || !Guid.TryParse(idProperty.GetString(), out var attachmentId))
            {
                return "GST certificate attachment is required.";
            }

            var exists = await db.VendorAttachments.AsNoTracking()
                .AnyAsync(existing => existing.Id == attachmentId && existing.Kind == VendorAttachmentKinds.GstCertificate, cancellationToken);
            return exists ? null : "Referenced GST certificate attachment was not found.";
        }
        catch (JsonException)
        {
            return "Attachment metadata is not valid JSON.";
        }
    }
}
