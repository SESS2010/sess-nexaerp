using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Api.Security;
using SESS.NexaERP.Application.Audit;
using SESS.NexaERP.Application.Masters;
using SESS.NexaERP.Domain.Masters;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Api.Endpoints;

public static class MasterEndpoints
{
    public static IEndpointRouteBuilder MapMasterEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/masters").WithTags("Masters").RequireAuthorization();

        group.MapGet("/customers", async (NexaErpDbContext db, int? page, int? pageSize, CancellationToken cancellationToken) =>
        {
            var paging = Paging.Normalize(page, pageSize);
            var customers = await db.Customers
                .AsNoTracking()
                .OrderBy(customer => customer.CustomerCode)
                .Skip(paging.Skip)
                .Take(paging.Take)
                .Select(customer => new CustomerSummary(customer.Id, customer.CustomerCode, customer.Name, customer.GstNumber, customer.PanNumber, customer.IsActive))
                .ToListAsync(cancellationToken);

            return Results.Ok(customers);
        });

        group.MapPost("/customers", async (CreateCustomerRequest request, NexaErpDbContext db, IAuditWriter audit, CancellationToken cancellationToken) =>
        {
            var code = NormalizeCode(request.CustomerCode);
            var gst = NormalizeOptional(request.GstNumber);
            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(request.Name))
            {
                return Results.BadRequest(new { message = "Customer code and name are required." });
            }

            if (await db.Customers.AnyAsync(customer => customer.CustomerCode == code || (gst != null && customer.GstNumber == gst), cancellationToken))
            {
                return Results.Conflict(new { message = "Duplicate customer code/GST blocked." });
            }

            var customer = new Customer
            {
                CustomerCode = code,
                Name = request.Name.Trim(),
                GstNumber = gst,
                PanNumber = NormalizeOptional(request.PanNumber)
            };

            db.Customers.Add(customer);
            await db.SaveChangesAsync(cancellationToken);
            await audit.WriteAsync("Masters", "Create", nameof(Customer), customer.Id.ToString(), null, customer, cancellationToken);

            return Results.Created($"/api/v1/masters/customers/{customer.Id}", new CustomerSummary(customer.Id, customer.CustomerCode, customer.Name, customer.GstNumber, customer.PanNumber, customer.IsActive));
        });

        group.MapGet("/vendors", async (NexaErpDbContext db, int? page, int? pageSize, CancellationToken cancellationToken) =>
        {
            var paging = Paging.Normalize(page, pageSize);
            var vendors = await db.Vendors
                .AsNoTracking()
                .OrderBy(vendor => vendor.VendorCode)
                .Skip(paging.Skip)
                .Take(paging.Take)
                .Select(vendor => new VendorSummary(vendor.Id, vendor.VendorCode, vendor.Name, vendor.GstNumber, vendor.PanNumber, vendor.ApprovalStatus, vendor.IsActive))
                .ToListAsync(cancellationToken);

            return Results.Ok(vendors);
        });

        group.MapPost("/vendors", async (CreateVendorRequest request, NexaErpDbContext db, IAuditWriter audit, CancellationToken cancellationToken) =>
        {
            var code = NormalizeCode(request.VendorCode);
            var gst = NormalizeOptional(request.GstNumber);
            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(request.Name))
            {
                return Results.BadRequest(new { message = "Vendor code and name are required." });
            }

            if (await db.Vendors.AnyAsync(vendor => vendor.VendorCode == code || (gst != null && vendor.GstNumber == gst), cancellationToken))
            {
                return Results.Conflict(new { message = "Duplicate vendor code/GST blocked." });
            }

            var vendor = new Vendor
            {
                VendorCode = code,
                Name = request.Name.Trim(),
                GstNumber = gst,
                PanNumber = NormalizeOptional(request.PanNumber),
                ApprovalStatus = "PendingTdApproval"
            };

            db.Vendors.Add(vendor);
            await db.SaveChangesAsync(cancellationToken);
            await audit.WriteAsync("Masters", "Create", nameof(Vendor), vendor.Id.ToString(), null, vendor, cancellationToken);

            return Results.Created($"/api/v1/masters/vendors/{vendor.Id}", new VendorSummary(vendor.Id, vendor.VendorCode, vendor.Name, vendor.GstNumber, vendor.PanNumber, vendor.ApprovalStatus, vendor.IsActive));
        });

        return endpoints;
    }

    private static string NormalizeCode(string value) => value.Trim().ToUpperInvariant();

    private static string? NormalizeOptional(string? value)
    {
        var trimmed = value?.Trim().ToUpperInvariant();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }
}

public readonly record struct Paging(int Skip, int Take)
{
    public static Paging Normalize(int? page, int? pageSize)
    {
        var safePage = Math.Max(page ?? 1, 1);
        var safePageSize = Math.Clamp(pageSize ?? 50, 1, 200);
        return new Paging((safePage - 1) * safePageSize, safePageSize);
    }
}
