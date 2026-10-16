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
    // Every barcode query and its permission filters share one captured Kolkata date.
    internal static IQueryable<EmployeeOperationalScope> Active(NexaErpDbContext db, ICurrentUser user, HttpContext context) =>
        PurchaseActorScope.ActiveScopes(db, user, BarcodeBusinessDate.ForRequest(context));
}
