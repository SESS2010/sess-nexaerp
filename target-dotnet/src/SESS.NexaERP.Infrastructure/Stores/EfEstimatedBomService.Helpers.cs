using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Stores;
using SESS.NexaERP.Domain.Inventory;
using SESS.NexaERP.Domain.Masters;
using SESS.NexaERP.Domain.Purchase;
using SESS.NexaERP.Domain.Stores;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Infrastructure.Stores;

public sealed partial class EfEstimatedBomService
{
    private sealed record MaterialLine(Guid ItemId, Guid UomId, decimal Quantity, string? Remarks, decimal? EstimatedUnitValue);

    private async Task<IReadOnlyList<MaterialLine>> MaterializeLinesAsync(Guid companyId, IReadOnlyList<EstimatedBomLineInput> lines, CancellationToken ct)
    {
        if (lines.Count == 0) throw new StoresValidationException("At least one Estimated BOM line is required.");
        var result = new List<MaterialLine>(lines.Count);
        foreach (var line in lines)
        {
            if (line.ItemId == Guid.Empty || line.UomId == Guid.Empty || line.Quantity <= 0 || line.EstimatedUnitValue < 0)
                throw new StoresValidationException("Every line requires ItemId, UomId and a positive Quantity.");
            var itemId = await TerminalItemIdAsync(line.ItemId, ct);
            var item = await db.Items.AsNoTracking().SingleOrDefaultAsync(x => x.Id == itemId, ct)
                ?? throw new StoresValidationException($"ItemId {line.ItemId} was not found.");
            if (!item.IsActive) throw new StoresValidationException($"Item {item.ItemCode} is inactive and cannot be copied into a Draft revision.");
            var uom = await db.Uoms.AsNoTracking().SingleOrDefaultAsync(x => x.Id == line.UomId && x.IsActive, ct)
                ?? throw new StoresValidationException($"UomId {line.UomId} is not active.");
            var baseUom = await db.Uoms.AsNoTracking().SingleAsync(x => x.Id == item.BaseUomId, ct);
            if (uom.MeasurementDimension != baseUom.MeasurementDimension)
                throw new StoresValidationException($"UOM {uom.Code} is not comparable with base UOM {baseUom.Code} for item {item.ItemCode}.");
            if (uom.Id != baseUom.Id)
            {
                var today = DateOnly.FromDateTime(DateTime.UtcNow);
                var conversion = await db.UomConversions.AnyAsync(x => x.IsActive && x.ApprovalStatus == MasterApprovalStatuses.Approved &&
                    x.MeasurementDimension == uom.MeasurementDimension && x.EffectiveFrom <= today &&
                    (!x.EffectiveTo.HasValue || x.EffectiveTo >= today) &&
                    ((x.FromUomId == uom.Id && x.ToUomId == baseUom.Id) || (x.FromUomId == baseUom.Id && x.ToUomId == uom.Id)), ct);
                if (!conversion) throw new StoresValidationException($"No effective approved UOM conversion relates {uom.Code} and {baseUom.Code}.");
            }
            result.Add(new(item.Id, uom.Id, line.Quantity, string.IsNullOrWhiteSpace(line.Remarks) ? null : line.Remarks.Trim(), line.EstimatedUnitValue));
        }
        return result;
    }

    private async Task<Guid> TerminalItemIdAsync(Guid itemId, CancellationToken ct)
    {
        var seen = new HashSet<Guid>();
        while (true)
        {
            if (!seen.Add(itemId)) throw new StoresConflictException("Item merge alias cycle detected.");
            var next = await db.ItemMergeAliases.AsNoTracking().Where(x => x.SourceItemId == itemId).Select(x => (Guid?)x.SurvivorItemId).SingleOrDefaultAsync(ct);
            if (!next.HasValue) return itemId;
            itemId = next.Value;
        }
    }

    private void AddLines(EstimatedBomRevision revision, Guid companyId, IReadOnlyList<MaterialLine> material)
    {
        var number = 0;
        foreach (var line in material)
            revision.Lines.Add(new EstimatedBomLine { CompanyId = companyId, EstimatedBomRevisionId = revision.Id,
                LineNumber = ++number, ItemId = line.ItemId, UomId = line.UomId, Quantity = line.Quantity,
                Remarks = line.Remarks, EstimatedUnitValue = line.EstimatedUnitValue,
                EstimatedUnitValueOverridden = line.EstimatedUnitValue.HasValue, CreatedBy = user.LoginId });
    }

    private static EstimatedBomRevision Current(EstimatedBom bom) =>
        bom.Revisions.Single(x => x.RevisionNumber == bom.CurrentRevisionNumber);

    private async Task ValidateSubmissionAsync(EstimatedBomRevision revision, CancellationToken ct)
    {
        if (revision.Lines.Count == 0) throw new StoresConflictException("An empty Estimated BOM cannot be submitted.");
        foreach (var line in revision.Lines)
        {
            var terminal = await TerminalItemIdAsync(line.ItemId, ct);
            var item = await db.Items.AsNoTracking().SingleAsync(x => x.Id == terminal, ct);
            if (!item.IsActive) throw new StoresConflictException($"Canonical item {item.ItemCode} is inactive; submission is blocked.");
        }
    }

    private async Task FreezeEstimatedValuesAsync(EstimatedBomRevision revision, CancellationToken ct)
    {
        foreach (var line in revision.Lines.Where(x => !x.EstimatedUnitValue.HasValue))
        {
            var terminalId = await TerminalItemIdAsync(line.ItemId, ct);
            var item = await db.Items.AsNoTracking().SingleAsync(x => x.Id == terminalId, ct);
            var purchase = await db.ItemCompanyLastPurchases.AsNoTracking()
                .SingleOrDefaultAsync(x => x.CompanyId == revision.CompanyId && x.ItemId == terminalId, ct);
            if (purchase?.LastPurchaseRate is null || purchase.LastPurchaseDate is null || purchase.LastPurchaseBillId is null)
                throw new StoresConflictException($"Item {item.ItemCode} has never been purchased in this company; provide an EstimatedUnitValue override before approval.");
            line.EstimatedUnitValue = await ConvertBaseUnitValueAsync(purchase.LastPurchaseRate.Value,
                item.BaseUomId, line.UomId, purchase.LastPurchaseDate.Value, item.ItemCode, ct);
            line.EstimatedUnitValueOverridden = false;
        }
    }

    private async Task<decimal> ConvertBaseUnitValueAsync(decimal baseUnitValue, Guid baseUomId,
        Guid lineUomId, DateOnly effectiveOn, string itemCode, CancellationToken ct)
    {
        if (baseUomId == lineUomId) return decimal.Round(baseUnitValue, 6);
        var conversion = await db.UomConversions.AsNoTracking().SingleOrDefaultAsync(x => x.IsActive &&
            x.ApprovalStatus == MasterApprovalStatuses.Approved && x.EffectiveFrom <= effectiveOn &&
            (!x.EffectiveTo.HasValue || x.EffectiveTo >= effectiveOn) &&
            ((x.FromUomId == lineUomId && x.ToUomId == baseUomId) ||
             (x.FromUomId == baseUomId && x.ToUomId == lineUomId)), ct)
            ?? throw new StoresConflictException($"No approved UOM conversion can price item {itemCode} in the Estimated BOM line UOM.");
        var baseQuantityPerLineUnit = conversion.FromUomId == lineUomId
            ? conversion.ConversionFactor : 1m / conversion.ConversionFactor;
        return decimal.Round(baseUnitValue * baseQuantityPerLineUnit, 6);
    }

    private async Task RequirePreparerAsync(CancellationToken ct)
    {
        var code = await db.Employees.AsNoTracking().Where(x => x.Id == Actor()).Select(x => x.EmployeeCode).SingleAsync(ct);
        var allowed = code is "SESS-04" or "SESS-05"
            ? new[] { "DESIGN_ENGINEER", "TECHNICAL_DIRECTOR", "TECHNICAL_SUPPORT_MANAGER", "SERVICE_ENGINEER" }
            : new[] { "DESIGN_ENGINEER", "TECHNICAL_DIRECTOR" };
        _ = user.RequireRole("create", allowed);
    }

    private void AddHistory(EstimatedBom bom, EstimatedBomRevision revision, string action,
        string? from, string to, string remarks, string correlation)
    {
        db.EstimatedBomHistories.Add(new EstimatedBomHistory
        {
            CompanyId = bom.CompanyId, EstimatedBomId = bom.Id, EstimatedBomRevisionId = revision.Id,
            Action = action, FromStatus = from, ToStatus = to, ActorEmployeeId = Actor(),
            ActorRoleCode = user.RoleCode, ResolvedRoleAssignmentId = user.ResolvedRoleAssignmentId!.Value,
            ResolvedRoleAssignmentType = user.ResolvedRoleAssignmentType!,
            CorrelationId = correlation, Remarks = remarks, CreatedBy = user.LoginId
        });
    }

    private async Task CommitCommandAsync(string organization, string operation, string key, object request,
        EstimatedBom bom, EstimatedBomRevision revision, CancellationToken ct)
    {
        var envelope = Rev869BCommandContextAuthorizer.CommandEnvelope.Create(organization, operation, key, request);
        var attempt = await Rev869BCommandContextAuthorizer.OpenForPendingChangesAsync(db, user, organization, envelope, ct, user.RoleCode)
            ?? throw new InvalidOperationException("Estimated BOM command produced no immutable operation slot.");
        await audit.WriteAsync("Design", operation, nameof(EstimatedBom), bom.Id.ToString(), null,
            new { bom.BomNumber, revision.RevisionNumber, revision.Status }, ct);
        await Rev869BCommandContextAuthorizer.StageCommittedReceiptAsync(db, attempt, ct);
    }

    private async Task<string> NextNumberAsync(Guid companyId, string organization, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var year = today.Month >= 4 ? $"{today.Year % 100:00}-{(today.Year + 1) % 100:00}" : $"{(today.Year - 1) % 100:00}-{today.Year % 100:00}";
        var lockKey = $"NUMBER:{organization}:{year}:EBOM";
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({lockKey},0))", ct);
        var sequence = await db.PurchaseNumberSequences.SingleOrDefaultAsync(x => x.CompanyId == companyId &&
            x.OrganizationId == organization && x.FinancialYear == year && x.Prefix == "EBOM" && x.IsActive, ct);
        if (sequence is null)
        {
            sequence = new PurchaseNumberSequence { CompanyId = companyId, OrganizationId = organization,
                FinancialYear = year, Prefix = "EBOM", CreatedBy = user.LoginId };
            db.PurchaseNumberSequences.Add(sequence);
        }
        sequence.LastNumber++; sequence.UpdatedAt = DateTimeOffset.UtcNow; sequence.UpdatedBy = user.LoginId;
        return $"EBOM-{organization}-{year}-{sequence.LastNumber:000001}";
    }

    private async Task<EstimatedBomView> ViewAsync(EstimatedBom bom, CancellationToken ct)
    {
        var revision = Current(bom);
        var itemIds = revision.Lines.Select(x => x.ItemId).Distinct().ToArray();
        var items = await db.Items.AsNoTracking().Where(x => itemIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
        var uomIds = revision.Lines.Select(x => x.UomId).Distinct().ToArray();
        var uoms = await db.Uoms.AsNoTracking().Where(x => uomIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
        var lines = new List<EstimatedBomLineView>(revision.Lines.Count);
        foreach (var line in revision.Lines.OrderBy(x => x.LineNumber))
        {
            var original = items[line.ItemId]; var terminalId = await TerminalItemIdAsync(line.ItemId, ct);
            var terminal = terminalId == original.Id ? original : await db.Items.AsNoTracking().SingleAsync(x => x.Id == terminalId, ct);
            lines.Add(new(line.Id, line.LineNumber, original.Id, original.ItemCode, terminal.Id, terminal.ItemCode,
                terminal.IsActive, terminal.ApprovalStatus, line.UomId, uoms[line.UomId].Code, line.Quantity, line.Remarks,
                line.EstimatedUnitValue, line.EstimatedUnitValueOverridden, "INR"));
        }
        var canonical = new List<EstimatedBomCanonicalLineView>();
        foreach (var group in lines.GroupBy(x => x.CanonicalItemId))
        {
            var item = await db.Items.AsNoTracking().SingleAsync(x => x.Id == group.Key, ct);
            var baseUom = await db.Uoms.AsNoTracking().SingleAsync(x => x.Id == item.BaseUomId, ct);
            decimal total = 0;
            foreach (var source in group)
                total += await ToBaseQuantityAsync(source.Quantity, source.UomId, baseUom.Id, ct);
            canonical.Add(new(item.Id, item.ItemCode, item.IsActive, item.ApprovalStatus, baseUom.Id, baseUom.Code,
                total, group.OrderBy(x => x.LineNumber).ToArray()));
        }
        return new(bom.Id, bom.BomNumber, bom.JobOrderId, bom.JobOrder!.JobOrderNumber, bom.CurrentRevisionNumber,
            bom.ApprovedRevisionId, bom.CommercialBaselineRevisionId, bom.Status, bom.Version,
            new(revision.Id, revision.RevisionNumber, revision.Status, revision.RevisionReason,
                revision.PreparedByEmployeeId, revision.SubmittedAt, revision.ApprovedAt, revision.ApprovedByEmployeeId,
                revision.ApprovalReason, revision.Version, lines, canonical.OrderBy(x => x.CanonicalItemCode).ToArray()));
    }

    private async Task<decimal> ToBaseQuantityAsync(decimal quantity, Guid fromUomId, Guid baseUomId, CancellationToken ct)
    {
        if (fromUomId == baseUomId) return quantity;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var conversion = await db.UomConversions.AsNoTracking().SingleAsync(x => x.IsActive &&
            x.ApprovalStatus == MasterApprovalStatuses.Approved && x.EffectiveFrom <= today &&
            (!x.EffectiveTo.HasValue || x.EffectiveTo >= today) &&
            ((x.FromUomId == fromUomId && x.ToUomId == baseUomId) ||
             (x.FromUomId == baseUomId && x.ToUomId == fromUomId)), ct);
        return conversion.FromUomId == fromUomId ? quantity * conversion.ConversionFactor : quantity / conversion.ConversionFactor;
    }
}
