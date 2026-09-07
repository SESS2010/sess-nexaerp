using Microsoft.EntityFrameworkCore;
using System.Data;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Stores;

namespace SESS.NexaERP.Infrastructure.Stores;

public sealed partial class EfProductionEngineeringService
{
    public async Task<ProductionBomView> PinProductionBomRevisionAsync(
        string number, PinProductionBomRevisionRequest request, CancellationToken ct)
    {
        _ = user.RequireRole("update", "PRODUCTION_MANAGER", "TECHNICAL_DIRECTOR");
        var key = Required(request.IdempotencyKey, "IdempotencyKey");
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var company = await CompanyAsync(ct);
        var bom = await BomQuery().SingleOrDefaultAsync(
            x => x.CompanyId == company.Id && x.BomNumber == Code(number), ct)
            ?? throw new KeyNotFoundException("Production BOM was not found.");
        if (bom.JobOrder!.Version != request.ExpectedJobOrderVersion)
            throw new DbUpdateConcurrencyException("Job Order Version is stale.");
        var revision = bom.Revisions.SingleOrDefault(x => x.Id == request.RevisionId)
            ?? throw new StoresConflictException("The requested revision does not belong to this machine.");
        if (revision.Status != "APPROVED")
            throw new StoresConflictException("Only an Approved Production BOM revision may be pinned.");
        var old = bom.JobOrder.PinnedProductionBomRevisionId;
        bom.JobOrder.PinnedProductionBomRevisionId = revision.Id;
        bom.JobOrder.UpdatedAt = DateTimeOffset.UtcNow; bom.JobOrder.UpdatedBy = user.LoginId;
        var reason = Required(request.Reason, "Reason");
        History(bom, revision, null, null, "PinToMachine",
            old.HasValue ? "PINNED" : null, "PINNED", reason, key);
        await CommitAsync(company.Code, "ProductionBom.Pin", key, request,
            "JobOrder", bom.JobOrderId, new { bom.BomNumber, revision.Id, previous = old }, ct);
        await tx.CommitAsync(ct); return await BomViewAsync(bom, ct);
    }
}
