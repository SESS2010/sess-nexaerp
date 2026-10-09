using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Application.Audit;
using SESS.NexaERP.Application.Authorization;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Purchase;
using SESS.NexaERP.Application.Stores;
using SESS.NexaERP.Domain.Purchase;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Api.Endpoints;

/// <summary>
/// R7 (approved by name 26 Sep): the data for the printed purchase order. The layout is the frontend's;
/// every figure comes from the snapshots taken when the PO was approved, never recalculated here.
/// Only an issued, closed or cancelled PO prints (a cancelled one says so), each print is audited.
/// The data itself is built by IPurchaseOrderPrintQuery, which the PO_ISSUED e-mail also uses.
/// </summary>
public static partial class Rev869BPurchaseEndpoints
{
    internal static readonly string[] PrintablePoStatuses = [Rev869BStatuses.Issued, Rev869BStatuses.Closed, Rev869BStatuses.Cancelled];

    private static async Task<IResult> PrintPo(string number, NexaErpDbContext db, ICurrentUser user, IRecordScopeAuthorizer scopes,
        IPagePermissionService permissions, [Microsoft.AspNetCore.Mvc.FromServices] IPurchaseOrderPrintQuery print, IAuditWriter audit, CancellationToken ct)
    {
        var row = await ScopePurchaseOrders(db.PurchaseOrders.AsNoTracking(), db, user)
            .SingleOrDefaultAsync(x => x.OrganizationId == user.OrganizationId && x.PoNumber == number.Trim().ToUpper() && x.IsCurrentVersion, ct);
        if (row is null) return await Missing(audit, "purchase.po", number, user, ct);
        if (!await permissions.HasPermissionAsync(user.RoleCodes, "purchase.po", PagePermissionActions.ViewCommercialValues, ct))
            return await Denied(audit, "purchase.po", number, user, ct);
        if (!PrintablePoStatuses.Contains(row.Status))
            throw new StoresConflictException($"Purchase order {row.PoNumber} is {row.Status}. Only an issued, closed or cancelled purchase order can be printed.");
        var view = await print.GetAsync(row.Id, ct) with { PrintedBy = user.LoginId };
        await audit.WriteAsync("Purchase", "PrintPurchaseOrder", nameof(PurchaseOrder), row.Id.ToString(), null,
            new { row.PoNumber, row.RevisionNumber, row.Status, row.TotalPayableValue }, ct);
        return Results.Ok(view);
    }
}
