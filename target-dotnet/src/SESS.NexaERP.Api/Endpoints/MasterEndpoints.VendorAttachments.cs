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
        // Next free vendor code in the VEN-### series, skipping existing records.
        group.MapGet("/vendors/next-code", async (NexaErpDbContext db, CancellationToken cancellationToken) =>
        {
            var codes = await db.Vendors.AsNoTracking()
                .Where(vendor => vendor.VendorCode.StartsWith("VEN-"))
                .Select(vendor => vendor.VendorCode)
                .ToListAsync(cancellationToken);
            var highest = codes
                .Select(code => int.TryParse(code["VEN-".Length..], out var number) ? number : 0)
                .DefaultIfEmpty(0)
                .Max();
            return Results.Ok(new { VendorCode = $"VEN-{highest + 1:D3}" });
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
                return Results.BadRequest(new { message = $"Attachment kind must be {VendorAttachmentKinds.BankLeaf} or {VendorAttachmentKinds.GstCertificate}." });
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
