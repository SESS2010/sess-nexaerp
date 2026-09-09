using Microsoft.EntityFrameworkCore;
using System.Data;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Stores;
using SESS.NexaERP.Domain.Masters;
using SESS.NexaERP.Domain.Stores;

namespace SESS.NexaERP.Infrastructure.Stores;

public sealed partial class EfProductionEngineeringService
{
    public Task<ProductionBomView> SubmitProductionBomAsync(
        string number, ProductionBomActionRequest request, CancellationToken ct)
    {
        _ = user.RequireRole("submit", "PRODUCTION_MANAGER", "DESIGN_ENGINEER", "TECHNICAL_DIRECTOR");
        return TransitionProductionBomAsync(number, request, false, ct);
    }

    public Task<ProductionBomView> ApproveProductionBomAsync(
        string number, ProductionBomActionRequest request, CancellationToken ct)
    {
        _ = user.RequireRole("approve", "TECHNICAL_DIRECTOR");
        return TransitionProductionBomAsync(number, request, true, ct);
    }

    private async Task<ProductionBomView> TransitionProductionBomAsync(
        string number, ProductionBomActionRequest request, bool approve, CancellationToken ct)
    {
        var key = Required(request.IdempotencyKey, "IdempotencyKey");
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var company = await CompanyAsync(ct);
        var bom = await BomQuery().SingleOrDefaultAsync(
            x => x.CompanyId == company.Id && x.BomNumber == Code(number), ct)
            ?? throw new KeyNotFoundException("Production BOM was not found.");
        var revision = bom.Revisions.Single(x => x.RevisionNumber == bom.CurrentRevisionNumber);
        var expected = approve ? "SUBMITTED" : "DRAFT";
        if (revision.Status != expected)
            throw new StoresConflictException("Production BOM is not in the required state.");
        if (revision.Version != request.ExpectedVersion)
            throw new DbUpdateConcurrencyException("Production BOM revision Version is stale.");
        if (approve && revision.PreparedByEmployeeId == Actor())
            throw new StoresConflictException("Nobody may approve their own Production BOM revision.");
        await ValidateLinesAsync(revision.Lines.Select(x =>
            new ProductionBomLineInput(x.ItemId, x.UomId, x.Quantity, x.Remarks)).ToArray(), true, ct);
        if (approve) await FreezeProductionValuesAsync(revision, ct);
        var from = revision.Status; revision.Status = approve ? "APPROVED" : "SUBMITTED";
        revision.Version = checked(revision.Version + 1); bom.Version = checked(bom.Version + 1);
        bom.Status = revision.Status;
        if (approve) {
            revision.ApprovedAt = DateTimeOffset.UtcNow;
            revision.ApprovedByEmployeeId = Actor();
            revision.ApprovalReason = Required(request.Remarks, "Remarks");
        } else revision.SubmittedAt = DateTimeOffset.UtcNow;
        var action = approve ? "Approve" : "Submit";
        History(bom, revision, null, null, action, from, revision.Status,
            Required(request.Remarks, "Remarks"), key);
        await CommitAsync(company.Code, "ProductionBom." + action, key, request,
            nameof(ProductionBom), bom.Id, new { bom.BomNumber, revision.RevisionNumber }, ct);
        await tx.CommitAsync(ct); return await BomViewAsync(bom, ct);
    }
    private async Task FreezeProductionValuesAsync(ProductionBomRevision revision, CancellationToken ct)
    {
        var sourceValues = await db.EstimatedBomLines.AsNoTracking()
            .Where(x => x.EstimatedBomRevisionId == revision.SourceEstimatedBomRevisionId && x.EstimatedUnitValue != null)
            .GroupBy(x => x.ItemId).Select(x => new { ItemId = x.Key, Value = x.First().EstimatedUnitValue!.Value })
            .ToDictionaryAsync(x => x.ItemId, x => x.Value, ct);
        foreach (var line in revision.Lines.Where(x => !x.PlannedUnitValue.HasValue))
        {
            if (sourceValues.TryGetValue(line.ItemId, out var value)) line.PlannedUnitValue = value;
            else
            {
                var item = await db.Items.AsNoTracking().SingleAsync(x => x.Id == line.ItemId, ct);
                line.PlannedUnitValue = item.StandardEstimatedPrice
                    ?? throw new StoresConflictException($"Item {item.ItemCode} has no approved Estimated BOM value or Standard Estimated Price.");
            }
        }
    }
}
