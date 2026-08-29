using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Api.Security;
using SESS.NexaERP.Application.Audit;
using SESS.NexaERP.Application.Authorization;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Domain.Inventory;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Api.Endpoints;

public static class ItemVendorEndpoints
{
    public sealed record SetItemVendorsRequest(IReadOnlyList<string> VendorCodes);

    private const long MaxImageBytes = 5 * 1024 * 1024;
    private static readonly string[] AllowedImageContentTypes = ["image/jpeg", "image/png", "image/webp"];

    public static IEndpointRouteBuilder MapItemVendorEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var inventory = endpoints.MapGroup("/api/v1/inventory").WithTags("Inventory").RequireAuthorization();

        // Vendors supplying an item.
        inventory.MapGet("/items/{code}/vendors", async (string code, NexaErpDbContext db, CancellationToken ct) =>
        {
            var item = await db.Items.AsNoTracking().SingleOrDefaultAsync(x => x.ItemCode == code.Trim().ToUpperInvariant(), ct);
            if (item is null) return Results.NotFound(new { message = "Item not found." });
            var vendors = await db.ItemVendors.AsNoTracking()
                .Where(link => link.ItemId == item.Id)
                .Select(link => new { link.Vendor!.VendorCode, link.Vendor.Name, link.Vendor.VendorStatus, link.Vendor.IsActive })
                .OrderBy(v => v.VendorCode)
                .ToListAsync(ct);
            return Results.Ok(vendors);
        }).RequirePagePermission("masters.items", PagePermissionActions.View);

        // Replace the vendor set for an item (multi-select save).
        inventory.MapPut("/items/{code}/vendors", async (string code, SetItemVendorsRequest request, NexaErpDbContext db, IAuditWriter audit, ICurrentUser user, CancellationToken ct) =>
        {
            var item = await db.Items.SingleOrDefaultAsync(x => x.ItemCode == code.Trim().ToUpperInvariant(), ct);
            if (item is null) return Results.NotFound(new { message = "Item not found." });

            var requestedCodes = (request.VendorCodes ?? [])
                .Select(vendorCode => vendorCode.Trim().ToUpperInvariant())
                .Where(vendorCode => vendorCode.Length > 0)
                .Distinct()
                .ToList();
            var vendors = await db.Vendors.Where(v => requestedCodes.Contains(v.VendorCode)).Select(v => new { v.Id, v.VendorCode }).ToListAsync(ct);
            var missing = requestedCodes.Except(vendors.Select(v => v.VendorCode)).ToList();
            if (missing.Count > 0) return Results.BadRequest(new { message = $"Unknown vendor codes: {string.Join(", ", missing)}" });

            var existing = await db.ItemVendors.Where(link => link.ItemId == item.Id).ToListAsync(ct);
            var before = existing.Select(link => link.VendorId).ToList();
            db.ItemVendors.RemoveRange(existing.Where(link => !vendors.Any(v => v.Id == link.VendorId)));
            foreach (var vendor in vendors.Where(v => !existing.Any(link => link.VendorId == v.Id)))
            {
                db.ItemVendors.Add(new ItemVendor { ItemId = item.Id, VendorId = vendor.Id, CreatedBy = user.LoginId });
            }
            await db.SaveChangesAsync(ct);
            await audit.WriteAsync("Masters", "SetItemVendors", nameof(Item), item.Id.ToString(),
                new { VendorIds = before }, new { VendorCodes = requestedCodes }, ct);
            return Results.Ok(new { item.ItemCode, VendorCodes = requestedCodes });
        }).RequirePagePermission("masters.items", PagePermissionActions.Update);

        // Item image upload/download. Item.ImageStorageKey references item_images.Id.
        inventory.MapPost("/items/{code}/image", async (string code, HttpRequest request, NexaErpDbContext db, IAuditWriter audit, ICurrentUser user, CancellationToken ct) =>
        {
            var item = await db.Items.SingleOrDefaultAsync(x => x.ItemCode == code.Trim().ToUpperInvariant(), ct);
            if (item is null) return Results.NotFound(new { message = "Item not found." });
            if (!request.HasFormContentType) return Results.BadRequest(new { message = "Multipart form data with a 'file' field is required." });
            var form = await request.ReadFormAsync(ct);
            var file = form.Files.GetFile("file");
            if (file is null || file.Length == 0) return Results.BadRequest(new { message = "An image file is required." });
            if (file.Length > MaxImageBytes) return Results.BadRequest(new { message = "Image exceeds the 5 MB limit." });
            if (!AllowedImageContentTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
                return Results.BadRequest(new { message = "Only JPEG, PNG or WebP images are accepted." });

            await using var stream = file.OpenReadStream();
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer, ct);

            var image = new ItemImage
            {
                FileName = Path.GetFileName(file.FileName),
                ContentType = file.ContentType,
                SizeBytes = file.Length,
                Content = buffer.ToArray(),
                CreatedBy = user.LoginId
            };
            db.ItemImages.Add(image);
            if (Guid.TryParse(item.ImageStorageKey, out var oldImageId))
            {
                await db.ItemImages.Where(x => x.Id == oldImageId).ExecuteDeleteAsync(ct);
            }
            item.ImageStorageKey = image.Id.ToString();
            item.ImageFileName = image.FileName;
            item.ImageContentType = image.ContentType;
            item.UpdatedAt = DateTimeOffset.UtcNow;
            item.UpdatedBy = user.LoginId;
            await db.SaveChangesAsync(ct);
            await audit.WriteAsync("Masters", "UploadImage", nameof(Item), item.Id.ToString(), null,
                new { image.FileName, image.SizeBytes }, ct);
            return Results.Ok(new { item.ItemCode, ImageStorageKey = item.ImageStorageKey, image.FileName });
        })
        .DisableAntiforgery()
        .RequirePagePermission("masters.items", PagePermissionActions.UploadAttachment);

        inventory.MapGet("/items/{code}/image", async (string code, NexaErpDbContext db, CancellationToken ct) =>
        {
            var item = await db.Items.AsNoTracking().SingleOrDefaultAsync(x => x.ItemCode == code.Trim().ToUpperInvariant(), ct);
            if (item is null || !Guid.TryParse(item.ImageStorageKey, out var imageId)) return Results.NotFound(new { message = "Item image not found." });
            var image = await db.ItemImages.AsNoTracking().SingleOrDefaultAsync(x => x.Id == imageId, ct);
            return image is null
                ? Results.NotFound(new { message = "Item image not found." })
                : Results.File(image.Content, image.ContentType, image.FileName);
        }).RequirePagePermission("masters.items", PagePermissionActions.View);

        // Reverse view: items supplied by a vendor (linked + preferred).
        var masters = endpoints.MapGroup("/api/v1/masters").WithTags("Masters").RequireAuthorization();
        masters.MapGet("/vendors/{vendorCode}/items", async (string vendorCode, NexaErpDbContext db, CancellationToken ct) =>
        {
            var vendor = await db.Vendors.AsNoTracking().SingleOrDefaultAsync(v => v.VendorCode == vendorCode.Trim().ToUpperInvariant(), ct);
            if (vendor is null) return Results.NotFound(new { message = "Vendor not found." });
            var linked = await db.ItemVendors.AsNoTracking()
                .Where(link => link.VendorId == vendor.Id)
                .Select(link => new { link.Item!.ItemCode, link.Item.Name, link.Item.Uom, link.Item.MaterialType, link.Item.Status, Relationship = "SUPPLIER" })
                .ToListAsync(ct);
            var preferred = await db.Items.AsNoTracking()
                .Where(item => item.PreferredVendorId == vendor.Id)
                .Select(item => new { item.ItemCode, item.Name, item.Uom, item.MaterialType, item.Status, Relationship = "PREFERRED" })
                .ToListAsync(ct);
            var all = linked.Concat(preferred.Where(p => !linked.Any(l => l.ItemCode == p.ItemCode))).OrderBy(x => x.ItemCode).ToList();
            return Results.Ok(all);
        }).RequirePagePermission("masters.vendors", PagePermissionActions.View);

        return endpoints;
    }
}
