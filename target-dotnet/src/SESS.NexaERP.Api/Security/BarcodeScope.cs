using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Domain.Authorization;
using SESS.NexaERP.Infrastructure.Authorization;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Api.Security;
internal static class BarcodeScope
{
    internal static bool RequireStores(ICurrentUser user, string operation)
    {
        try { user.RequireRole(operation, "STORES_EXECUTIVE", "STORES_ASSISTANT", "STORES_MANAGER"); return true; }
        catch (Exception error) when (error is UnauthorizedAccessException or ArgumentException) { return false; }
    }
    // Use the database business date for barcode metadata; no UTC/local midnight split.
    internal static async Task<IQueryable<EmployeeOperationalScope>> Active(NexaErpDbContext db, ICurrentUser user, CancellationToken ct)
    {
        var today = await db.Database.SqlQueryRaw<DateOnly>("SELECT CURRENT_DATE AS \"Value\"").SingleAsync(ct);
        return PurchaseActorScope.ActiveScopes(db, user, today);
    }
}