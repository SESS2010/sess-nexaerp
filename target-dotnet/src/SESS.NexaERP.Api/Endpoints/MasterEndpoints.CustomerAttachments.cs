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
    private static void MapCustomerAttachmentEndpoints(RouteGroupBuilder group)
    {
        // Next free customer code, continuing the imported CUST-######## series.
        group.MapGet("/customers/next-code", async (NexaErpDbContext db, CancellationToken cancellationToken) =>
        {
            var codes = await db.Customers.AsNoTracking()
                .Where(customer => customer.CustomerCode.StartsWith("CUST-"))
                .Select(customer => customer.CustomerCode)
                .ToListAsync(cancellationToken);
            var highest = codes
                .Select(code => int.TryParse(code["CUST-".Length..], out var number) ? number : 0)
                .DefaultIfEmpty(0)
                .Max();
            return Results.Ok(new { CustomerCode = $"CUST-{highest + 1:D8}" });
        }).RequirePagePermission("masters.customers", PagePermissionActions.Create);

        group.MapPost("/customers/attachments", async (HttpRequest request, NexaErpDbContext db, IAuditWriter audit, ICurrentUser currentUser, CancellationToken cancellationToken) =>
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
            if (!CustomerAttachmentKinds.IsValid(kind))
            {
                return Results.BadRequest(new { message = $"Attachment kind must be {CustomerAttachmentKinds.GstCertificate}, {CustomerAttachmentKinds.BankLeaf}, {CustomerAttachmentKinds.MsmeCertificate} or {CustomerAttachmentKinds.PanCard}." });
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

            var attachment = new CustomerAttachment
            {
                Kind = kind,
                FileName = Path.GetFileName(file.FileName),
                ContentType = file.ContentType,
                SizeBytes = file.Length,
                Content = buffer.ToArray(),
                CreatedBy = currentUser.LoginId
            };
            db.CustomerAttachments.Add(attachment);
            await db.SaveChangesAsync(cancellationToken);
            await audit.WriteAsync("Masters", "UploadAttachment", nameof(CustomerAttachment), attachment.Id.ToString(), null,
                new { attachment.Kind, attachment.FileName, attachment.SizeBytes }, cancellationToken);

            return Results.Created($"/api/v1/masters/customers/attachments/{attachment.Id}",
                new { attachment.Id, attachment.Kind, attachment.FileName, attachment.ContentType, attachment.SizeBytes });
        })
        .DisableAntiforgery()
        .RequirePagePermission("masters.customers", PagePermissionActions.UploadAttachment);

        group.MapGet("/customers/attachments/{attachmentId:guid}", async (Guid attachmentId, NexaErpDbContext db, CancellationToken cancellationToken) =>
        {
            var attachment = await db.CustomerAttachments.AsNoTracking()
                .SingleOrDefaultAsync(existing => existing.Id == attachmentId, cancellationToken);
            return attachment is null
                ? Results.NotFound(new { message = "Attachment not found." })
                : Results.File(attachment.Content, attachment.ContentType, attachment.FileName);
        }).RequirePagePermission("masters.customers", PagePermissionActions.Download);
    }
}
