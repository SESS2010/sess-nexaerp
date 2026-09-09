using Microsoft.EntityFrameworkCore;
using System.Data;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Stores;
using SESS.NexaERP.Domain.Stores;

namespace SESS.NexaERP.Infrastructure.Stores;

public sealed partial class EfProductionEngineeringService
{
    public async Task<ProductionBomView> CreateProductionBomAsync(
        CreateProductionBomRequest request, CancellationToken ct)
    {
        _ = user.RequireRole("create", "PRODUCTION_MANAGER", "DESIGN_ENGINEER", "TECHNICAL_DIRECTOR");
        var key = Required(request.IdempotencyKey, "IdempotencyKey");
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var company = await CompanyAsync(ct);
        var replay = await db.ProductionBomRevisions.SingleOrDefaultAsync(
            x => x.CompanyId == company.Id && x.IdempotencyKey == key, ct);
        if (replay is not null) {
            if (replay.ContentFingerprint != Fingerprint(request))
                throw new StoresConflictException("Idempotency key was reused with different Production BOM content.");
            var old = await BomQuery().SingleAsync(x => x.Id == replay.ProductionBomId, ct);
            await tx.CommitAsync(ct); return await BomViewAsync(old, ct);
        }
        var job = await db.JobOrders.SingleOrDefaultAsync(
            x => x.CompanyId == company.Id && x.Id == request.JobOrderId, ct)
            ?? throw new KeyNotFoundException("Job Order was not found in the selected company.");
        if (job.Status != "OPEN") throw new StoresConflictException("Accounts must confirm the Job Order before a Production BOM is created.");
        if (await db.ProductionBoms.AnyAsync(x => x.CompanyId == company.Id && x.JobOrderId == job.Id, ct))
            throw new StoresConflictException("This Job Order already has a Production BOM.");
        var estimated = await db.EstimatedBoms.Include(x => x.Revisions).ThenInclude(x => x.Lines)
            .SingleOrDefaultAsync(x => x.CompanyId == company.Id && x.JobOrderId == job.Id, ct)
            ?? throw new StoresConflictException("An Estimated BOM is required before creating the Production BOM.");
        if (!estimated.ApprovedRevisionId.HasValue)
            throw new StoresConflictException("The Estimated BOM must have an Approved revision.");
        var source = estimated.Revisions.Single(x => x.Id == estimated.ApprovedRevisionId.Value);
        var bom = new ProductionBom { CompanyId = company.Id,
            BomNumber = await NextNumberAsync(company.Id, company.Code, "PBOM", ct),
            JobOrderId = job.Id, CurrentRevisionNumber = 1, CreatedBy = user.LoginId };
        var revision = NewProductionRevision(bom, source, null, 1,
            Required(request.RevisionReason, "RevisionReason"), key, Fingerprint(request));
        foreach (var line in revision.Lines)
            line.ItemId = await CanonicalItemAsync(line.ItemId, ct);
        bom.Revisions.Add(revision); db.ProductionBoms.Add(bom);
        History(bom, revision, null, null, "CreateFromEstimated", null, "DRAFT",
            revision.RevisionReason, key);
        await CommitAsync(company.Code, "ProductionBom.Create", key, request,
            nameof(ProductionBom), bom.Id, new { bom.BomNumber, sourceRevisionId = source.Id }, ct);
        await tx.CommitAsync(ct); return await BomViewAsync(bom, ct);
    }

    private ProductionBomRevision NewProductionRevision(ProductionBom bom,
        EstimatedBomRevision source, ProductionBomRevision? prior, int number,
        string reason, string key, string fingerprint)
    {
        var revision = new ProductionBomRevision { CompanyId = bom.CompanyId,
            ProductionBomId = bom.Id, RevisionNumber = number,
            SourceEstimatedBomRevisionId = source.Id, SupersedesRevisionId = prior?.Id,
            RevisionReason = reason, PreparedByEmployeeId = Actor(), IdempotencyKey = key,
            ContentFingerprint = fingerprint, CreatedBy = user.LoginId };
        var line = 0;
        foreach (var sourceLine in (prior?.Lines.Select(x => (x.ItemId, x.UomId, x.Quantity, x.Remarks, UnitValue: x.PlannedUnitValue))
            ?? source.Lines.Select(x => (x.ItemId, x.UomId, x.Quantity, x.Remarks, UnitValue: x.EstimatedUnitValue)))) {
            revision.Lines.Add(new ProductionBomLine { CompanyId = bom.CompanyId,
                ProductionBomRevisionId = revision.Id, LineNumber = ++line,
                ItemId = sourceLine.ItemId, UomId = sourceLine.UomId,
                Quantity = sourceLine.Quantity, Remarks = sourceLine.Remarks, PlannedUnitValue = sourceLine.UnitValue,
                CreatedBy = user.LoginId });
        }
        return revision;
    }
}
