using Microsoft.EntityFrameworkCore;
using System.Data;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Stores;
using SESS.NexaERP.Domain.Stores;

namespace SESS.NexaERP.Infrastructure.Stores;

public sealed partial class EfProductionEngineeringService
{
    public async Task<ProductionBomView> ReplaceProductionBomAsync(
        string number, ReplaceProductionBomRequest request, CancellationToken ct)
    {
        _ = user.RequireRole("update", "PRODUCTION_MANAGER", "DESIGN_ENGINEER", "TECHNICAL_DIRECTOR");
        var key = Required(request.IdempotencyKey, "IdempotencyKey");
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var company = await CompanyAsync(ct);
        var bom = await BomQuery().SingleOrDefaultAsync(
            x => x.CompanyId == company.Id && x.BomNumber == Code(number), ct)
            ?? throw new KeyNotFoundException("Production BOM was not found.");
        var revision = bom.Revisions.Single(x => x.RevisionNumber == bom.CurrentRevisionNumber);
        if (revision.Status != "DRAFT") throw new StoresConflictException("Only a Draft revision can be edited.");
        if (revision.Version != request.ExpectedVersion)
            throw new DbUpdateConcurrencyException("Production BOM revision Version is stale.");
        var lines = await ValidateLinesAsync(request.Lines, false, ct);
        var existingValues = revision.Lines.GroupBy(x => x.ItemId).ToDictionary(x => x.Key, x => x.First().PlannedUnitValue);
        db.ProductionBomLines.RemoveRange(revision.Lines); revision.Lines.Clear();
        var lineNumber = 0;
        foreach (var line in lines) revision.Lines.Add(new ProductionBomLine {
            CompanyId = company.Id, ProductionBomRevisionId = revision.Id,
            LineNumber = ++lineNumber, ItemId = line.ItemId, UomId = line.UomId,
            Quantity = line.Quantity, Remarks = line.Remarks, PlannedUnitValue = existingValues.GetValueOrDefault(line.ItemId),
            CreatedBy = user.LoginId
        });
        db.ProductionBomLines.AddRange(revision.Lines);
        revision.RevisionReason = Required(request.RevisionReason, "RevisionReason");
        revision.Version = checked(revision.Version + 1);
        revision.ContentFingerprint = Fingerprint(request);
        History(bom, revision, null, null, "Update", "DRAFT", "DRAFT",
            revision.RevisionReason, key);
        await CommitAsync(company.Code, "ProductionBom.Update", key, request,
            nameof(ProductionBom), bom.Id, new { bom.BomNumber, revision.RevisionNumber }, ct);
        await tx.CommitAsync(ct); return await BomViewAsync(bom, ct);
    }
}
