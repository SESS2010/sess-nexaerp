using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Application.Stores;
using SESS.NexaERP.Domain.Masters;

namespace SESS.NexaERP.Infrastructure.Stores;

public sealed partial class EfProductionEngineeringService
{
    private async Task<Guid> CanonicalItemAsync(Guid itemId, CancellationToken ct)
    {
        var seen = new HashSet<Guid>();
        while (true) {
            if (!seen.Add(itemId)) throw new StoresConflictException("Item merge alias cycle detected.");
            var next = await db.ItemMergeAliases.AsNoTracking().Where(x => x.SourceItemId == itemId)
                .Select(x => (Guid?)x.SurvivorItemId).SingleOrDefaultAsync(ct);
            if (!next.HasValue) return itemId;
            itemId = next.Value;
        }
    }

    private async Task<IReadOnlyList<ProductionBomLineInput>> ValidateLinesAsync(
        IReadOnlyList<ProductionBomLineInput> lines, bool submission, CancellationToken ct)
    {
        if (lines.Count == 0) throw new StoresValidationException("At least one Production BOM line is required.");
        var result = new List<ProductionBomLineInput>(lines.Count);
        foreach (var line in lines) {
            if (line.ItemId == Guid.Empty || line.UomId == Guid.Empty || line.Quantity <= 0)
                throw new StoresValidationException("Every line requires ItemId, UomId and a positive Quantity.");
            var itemId = await CanonicalItemAsync(line.ItemId, ct);
            var item = await db.Items.AsNoTracking().SingleOrDefaultAsync(x => x.Id == itemId, ct)
                ?? throw new StoresValidationException("Item was not found.");
            if (!item.IsActive || submission && item.ApprovalStatus != MasterApprovalStatuses.Approved)
                throw new StoresConflictException("Production BOM submission requires active Approved items.");
            var uom = await db.Uoms.AsNoTracking().SingleOrDefaultAsync(x => x.Id == line.UomId && x.IsActive, ct)
                ?? throw new StoresValidationException("UOM was not found or active.");
            var baseUom = await db.Uoms.AsNoTracking().SingleAsync(x => x.Id == item.BaseUomId, ct);
            if (uom.MeasurementDimension != baseUom.MeasurementDimension)
                throw new StoresValidationException("Line UOM is not comparable with the Item base UOM.");
            if (uom.Id != baseUom.Id) {
                var today = DateOnly.FromDateTime(DateTime.UtcNow);
                var conversion = await db.UomConversions.AsNoTracking().AnyAsync(x =>
                    x.IsActive && x.ApprovalStatus == MasterApprovalStatuses.Approved &&
                    x.EffectiveFrom <= today && (!x.EffectiveTo.HasValue || x.EffectiveTo >= today) &&
                    ((x.FromUomId == uom.Id && x.ToUomId == baseUom.Id) ||
                     (x.FromUomId == baseUom.Id && x.ToUomId == uom.Id)), ct);
                if (!conversion)
                    throw new StoresValidationException("No effective approved UOM conversion exists for the line.");
            }
            result.Add(line with { ItemId = itemId, Remarks = string.IsNullOrWhiteSpace(line.Remarks) ? null : line.Remarks.Trim() });
        }
        return result;
    }
}
