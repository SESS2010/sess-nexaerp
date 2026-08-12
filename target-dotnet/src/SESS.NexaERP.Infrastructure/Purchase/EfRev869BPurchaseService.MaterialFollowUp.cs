using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Application.Purchase;
using SESS.NexaERP.Domain.Authorization;
using SESS.NexaERP.Domain.Purchase;

namespace SESS.NexaERP.Infrastructure.Purchase;

public sealed partial class EfRev869BPurchaseService
{
    public async Task<Rev869BDocumentResult> TransitionMaterialFollowUpAsync(
        Guid handoffId, Rev869BMaterialFollowUpTransitionRequest request, CancellationToken ct)
    {
        var actor = RequireActor();
        RequireRole(Rev869ARoleCodes.StoresExecutive, Rev869ARoleCodes.StoresManager);
        if (string.IsNullOrWhiteSpace(request.Reason) || string.IsNullOrWhiteSpace(request.IdempotencyKey))
            throw new Rev869BValidationException("Material Follow-up reason and idempotency key are required.");
        if (request.ToStatus is not (Rev869BStatuses.InProgress or Rev869BStatuses.Completed))
            throw new Rev869BValidationException("Material Follow-up target must be InProgress or Completed.");

        await using var tx = await BeginTransactionScopeAsync(ct);
        var organization = RequireOrganization();
        var handoff = await db.MaterialFollowUpHandoffs.AsNoTracking().Include(x => x.PurchaseOrder)
            .SingleOrDefaultAsync(x => x.Id == handoffId && x.PurchaseOrder!.OrganizationId == organization, ct)
            ?? throw new Rev869BNotFoundException("Material Follow-up handoff was not found in the current organization.");
        var po = handoff.PurchaseOrder!;
        await RequireScopeAsync(actor, organization, po.RequestingDepartmentId, po.DeliveryWarehouseId, null, po.OwnerEmployeeId, ct);
        if (po.Status != Rev869BStatuses.Issued || !po.IsCurrentVersion)
            throw new Rev869BConflictException("Material Follow-up requires the exact current issued purchase order.");
        var allowed = handoff.Status == Rev869BStatuses.PendingFollowUp && request.ToStatus == Rev869BStatuses.InProgress ||
                      handoff.Status == Rev869BStatuses.InProgress && request.ToStatus == Rev869BStatuses.Completed;
        if (!allowed) throw new Rev869BConflictException("Illegal Material Follow-up lifecycle transition.");

        var next = checked(request.Version + 1);
        var fingerprint = Rev869BIdempotencyFingerprint.Create(organization, "MaterialFollowUp", request.IdempotencyKey,
            new { handoffId, request.ToStatus, reason = request.Reason.Trim(), request.Version });
        var affected = await db.MaterialFollowUpHandoffs
            .Where(x => x.Id == handoffId && x.Version == request.Version && x.Status == handoff.Status)
            .ExecuteUpdateAsync(update => update
                .SetProperty(x => x.Version, next)
                .SetProperty(x => x.Status, request.ToStatus)
                .SetProperty(x => x.CorrelationId, fingerprint)
                .SetProperty(x => x.UpdatedAt, DateTimeOffset.UtcNow)
                .SetProperty(x => x.UpdatedBy, user.LoginId), ct);
        RequireCas(affected, request.Version, "material follow-up");
        AddStatus("MaterialFollowUp", handoff.Id, handoff.HandoffNumber, handoff.Status, request.ToStatus,
            request.ToStatus == Rev869BStatuses.InProgress ? "StartFollowUp" : "CompleteFollowUp",
            request.Reason.Trim(), fingerprint);
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync("Purchase", "MaterialFollowUpTransition", nameof(MaterialFollowUpHandoff), handoffId.ToString(),
            new { handoff.Status, handoff.Version }, new { request.ToStatus, Version = next, Reason = request.Reason.Trim() }, ct);
        await tx.CommitAsync(ct);
        return Result(handoff.Id, handoff.HandoffNumber, request.ToStatus, next);
    }
}
