using Microsoft.EntityFrameworkCore;
using System.Data;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Stores;

namespace SESS.NexaERP.Infrastructure.Stores;

public sealed partial class EfProductionEngineeringService
{
    public async Task<ProductionBomView> CreateProductionBomRevisionAsync(
        string number, NewProductionBomRevisionRequest request, CancellationToken ct)
    {
        _ = user.RequireRole("create", "PRODUCTION_MANAGER", "DESIGN_ENGINEER", "TECHNICAL_DIRECTOR");
        var key = Required(request.IdempotencyKey, "IdempotencyKey");
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var company = await CompanyAsync(ct);
        var replay = await db.ProductionBomRevisions.SingleOrDefaultAsync(
            x => x.CompanyId == company.Id && x.IdempotencyKey == key, ct);
        if (replay is not null) {
            if (replay.ContentFingerprint != Fingerprint(request))
                throw new StoresConflictException("Idempotency key was reused with different revision content.");
            var existing = await BomQuery().SingleAsync(x => x.Id == replay.ProductionBomId, ct);
            await tx.CommitAsync(ct); return await BomViewAsync(existing, ct);
        }
        var bom = await BomQuery().SingleOrDefaultAsync(
            x => x.CompanyId == company.Id && x.BomNumber == Code(number), ct)
            ?? throw new KeyNotFoundException("Production BOM was not found.");
        if (bom.Version != request.ExpectedBomVersion)
            throw new DbUpdateConcurrencyException("Production BOM Version is stale.");
        var prior = bom.Revisions.Single(x => x.RevisionNumber == bom.CurrentRevisionNumber);
        if (prior.Status != "APPROVED")
            throw new StoresConflictException("A new revision can only follow an Approved revision.");
        var estimated = await db.EstimatedBomRevisions.Include(x => x.Lines)
            .SingleAsync(x => x.Id == prior.SourceEstimatedBomRevisionId, ct);
        var revision = NewProductionRevision(bom, estimated, prior,
            prior.RevisionNumber + 1, Required(request.RevisionReason, "RevisionReason"),
            key, Fingerprint(request));
        foreach (var line in revision.Lines)
            line.ItemId = await CanonicalItemAsync(line.ItemId, ct);
        bom.Revisions.Add(revision); db.ProductionBomRevisions.Add(revision);
        bom.CurrentRevisionNumber = revision.RevisionNumber;
        bom.Status = "DRAFT"; bom.Version = checked(bom.Version + 1);
        History(bom, revision, null, null, "NewRevision", prior.Status, "DRAFT",
            revision.RevisionReason, key);
        await CommitAsync(company.Code, "ProductionBom.NewRevision", key, request,
            "ProductionBom", bom.Id, new { bom.BomNumber, revision.RevisionNumber }, ct);
        await tx.CommitAsync(ct); return await BomViewAsync(bom, ct);
    }
}
