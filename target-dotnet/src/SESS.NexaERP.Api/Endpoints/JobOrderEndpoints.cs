using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Api.Security;
using SESS.NexaERP.Application.Authorization;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Stores;
using SESS.NexaERP.Domain.Sales;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Api.Endpoints;

public static class JobOrderEndpoints
{
    /// <summary>
    /// One current-revision Customer PO line a Job Order may be raised against.
    /// Production roles hold no sales.customer-po grant, so this read is scoped
    /// to the Job Order page instead of the Customer PO one.
    /// </summary>
    public sealed record JobOrderCustomerPoLineLookup(Guid CustomerPurchaseOrderLineId, Guid CustomerPurchaseOrderId,
        string PoRecordNumber, string CustomerPoNumber, string CustomerName, string WorkStatus, int RevisionNumber,
        int SlNo, string Description, Guid ItemId, string ItemCode, decimal? Quantity, string? Uom,
        IReadOnlyList<int> TakenOrdinals);

    public static IEndpointRouteBuilder MapJobOrderEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/production/job-orders").WithTags("Job Orders").RequireAuthorization();
        group.MapGet("/customer-po-lines", async (string? search, NexaErpDbContext db, ICurrentUser user, CancellationToken ct) =>
        {
            var organizationId = user.OrganizationId?.Trim();
            var query = db.CustomerPurchaseOrderLines.AsNoTracking()
                .Where(line => line.CustomerPurchaseOrder!.Company != null
                    && line.CustomerPurchaseOrder.Company.Code == organizationId
                    && line.RevisionNumber == line.CustomerPurchaseOrder.CurrentRevisionNumber
                    && line.CustomerPurchaseOrder.WorkStatus != CustomerPoWorkStatuses.Completed);
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToUpperInvariant();
                query = query.Where(line => line.CustomerPurchaseOrder!.PoRecordNumber.ToUpper().Contains(term)
                    || line.CustomerPurchaseOrder.CustomerPoNumber.ToUpper().Contains(term)
                    || (line.CustomerPurchaseOrder.Customer != null && line.CustomerPurchaseOrder.Customer.Name.ToUpper().Contains(term))
                    || line.Description.ToUpper().Contains(term)
                    || (line.Item != null && line.Item.ItemCode.ToUpper().Contains(term)));
            }
            var rows = await query
                .OrderBy(line => line.CustomerPurchaseOrder!.PoRecordNumber).ThenBy(line => line.SlNo)
                .Take(50)
                .Select(line => new JobOrderCustomerPoLineLookup(line.Id, line.CustomerPurchaseOrderId,
                    line.CustomerPurchaseOrder!.PoRecordNumber, line.CustomerPurchaseOrder.CustomerPoNumber,
                    line.CustomerPurchaseOrder.Customer!.Name, line.CustomerPurchaseOrder.WorkStatus, line.RevisionNumber,
                    line.SlNo, line.Description, line.ItemId, line.Item!.ItemCode, line.Quantity, line.Uom,
                    db.JobOrders.Where(job => job.CustomerPurchaseOrderLineId == line.Id && job.MachineOrdinal != null)
                        .Select(job => job.MachineOrdinal!.Value).OrderBy(ordinal => ordinal).ToList()))
                .ToListAsync(ct);
            return Results.Ok(rows);
        }).RequirePagePermission("production.job-orders", PagePermissionActions.Create);
        group.MapGet("/", (int? page, int? pageSize, string? search, string? status, IJobOrderService service, CancellationToken ct) =>
            service.ListAsync(page, pageSize, search, status, ct)).RequirePagePermission("production.job-orders", PagePermissionActions.View);
        group.MapGet("/customer-po-lines", (IJobOrderService service, CancellationToken ct) =>
            service.CustomerPoLinesAsync(ct))
            .RequirePagePermission("production.job-orders", PagePermissionActions.View);
        group.MapGet("/{id:guid}", async (Guid id, IJobOrderService service, CancellationToken ct) =>
            await service.GetAsync(id, ct) is { } value ? Results.Ok(value) : Results.NotFound())
            .RequirePagePermission("production.job-orders", PagePermissionActions.View);
        group.MapGet("/{id:guid}/history", (Guid id, IJobOrderService service, CancellationToken ct) =>
            service.HistoryAsync(id, ct)).RequirePagePermission("production.job-orders", PagePermissionActions.ViewAuditHistory);
        group.MapPost("/", async (CreateJobOrderRequest request, IJobOrderService service, CancellationToken ct) =>
            Results.Created("", await service.CreateAsync(request, ct)))
            .RequirePagePermission("production.job-orders", PagePermissionActions.Create);
        group.MapPost("/{id:guid}/accounts-confirm", (Guid id, ConfirmJobOrderRequest request, IJobOrderService service, CancellationToken ct) =>
            service.ConfirmAccountsAsync(id, request, ct))
            .RequirePagePermission("production.job-orders", PagePermissionActions.Verify);
        group.MapPost("/{id:guid}/return-to-draft", (Guid id, ConfirmJobOrderRequest request, IJobOrderService service, CancellationToken ct) =>
            service.ReturnToDraftAsync(id, request, ct))
            .RequirePagePermission("production.job-orders", PagePermissionActions.Reject);
        group.MapPut("/{id:guid}/draft", (Guid id, ReviseDraftJobOrderRequest request, IJobOrderService service, CancellationToken ct) =>
            service.ReviseDraftAsync(id, request, ct))
            .RequirePagePermission("production.job-orders", PagePermissionActions.Update);
        group.MapPost("/{id:guid}/resubmit", (Guid id, ConfirmJobOrderRequest request, IJobOrderService service, CancellationToken ct) =>
            service.ResubmitAsync(id, request, ct))
            .RequirePagePermission("production.job-orders", PagePermissionActions.Submit);
        return endpoints;
    }
}