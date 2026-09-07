using System.Data;
using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Application.Audit;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Purchase;
using SESS.NexaERP.Domain.Inventory;
using SESS.NexaERP.Domain.Purchase;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Api.Endpoints;

public static partial class PurchaseRequisitionEndpoints
{
    private sealed record StockLocation(Guid WarehouseId, string WarehouseCode, Guid? RackBinId, string? RackBinCode, string LocationKey);

    private static async Task<IResult> StockCheck(string prNumber, StockCheckRequest request, NexaErpDbContext db, ICurrentUser user, IAuditWriter audit, CancellationToken ct)
    {
        var pr = await Scope(IncludeDetail(db.PurchaseRequisitions), user, db).SingleOrDefaultAsync(x => x.PrNumber == NormalizePr(prNumber), ct);
        if (pr is null) return Results.NotFound(new { message = "Purchase requisition not found." });
        if (pr.Status != PurchaseRequisitionStatuses.StockCheckPending) return Results.Conflict(new { message = "Stock check is allowed only after PR approval." });
        if (request.Version != pr.Version) return Results.Conflict(new { message = "Stale record version. Refresh and retry." });
        if (string.IsNullOrWhiteSpace(request.Remarks)) return Results.BadRequest(new { message = "Remarks are required." });
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var correlation = string.IsNullOrWhiteSpace(request.IdempotencyKey) ? Correlation("STOCKCHECK") : request.IdempotencyKey.Trim();
        var existingCheck = await db.StockAvailabilityChecks.SingleOrDefaultAsync(x => x.PurchaseRequisitionId == pr.Id && x.CorrelationId == correlation, ct);
        if (existingCheck is not null) return Results.Ok(new { existingCheck.CheckNumber, existingCheck.ResultStatus });
        var check = new StockAvailabilityCheck { CompanyId = pr.CompanyId, PurchaseRequisitionId = pr.Id, PurchaseRequisition = pr, CheckNumber = $"SC-{pr.PrNumber}-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}", CheckedBy = user.LoginId, Remarks = request.Remarks.Trim(), CorrelationId = correlation, CreatedBy = user.LoginId };
        var anyShortage = false;
        var anyReservation = false;
        foreach (var line in pr.Lines.OrderBy(x => x.LineNumber))
        {
            if (line.ItemId is null) return Results.BadRequest(new { message = $"Line {line.LineNumber} requires controlled New Item Request before PR stock check." });
            List<StockLocation> locations;
            try
            {
                locations = await ResolveLocations(line, pr, request, db, ct);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
            if (locations.Count == 0) return Results.BadRequest(new { message = $"Line {line.LineNumber}: at least one active stock-check warehouse is required." });
            var checkedAt = DateTimeOffset.UtcNow;
            var totalOnHand = 0m;
            var totalPriorReserved = 0m;
            var totalAvailable = 0m;
            var totalReservedNow = 0m;
            foreach (var location in locations)
            {
                var alreadyReservedForLine = await ActiveReservedForLine(db, line.Id, ct);
                var remainingForLine = Math.Max(line.RequestedQuantity - alreadyReservedForLine - totalReservedNow, 0);
                var onHand = await OnHand(db, line.ItemId.Value, location.WarehouseId, location.RackBinId, ct);
                var activeReserved = await ActiveReserved(db, line.ItemId.Value, location.WarehouseId, location.RackBinId, ct);
                var available = AvailableQuantity(onHand, activeReserved);
                var reserve = ReserveQuantity(remainingForLine, available);
                var locationShortage = ShortageQuantity(remainingForLine, reserve);
                totalOnHand += onHand;
                totalPriorReserved += activeReserved;
                totalAvailable += available;
                totalReservedNow += reserve;
                check.Lines.Add(new StockAvailabilityCheckLine { CompanyId = pr.CompanyId, PurchaseRequisitionLineId = line.Id, ItemId = line.ItemId.Value, WarehouseId = location.WarehouseId, RackBinId = location.RackBinId, LocationKey = location.LocationKey, RequestedQuantity = line.RequestedQuantity, OnHandQuantity = onHand, ActiveReservedQuantity = activeReserved, AvailableQuantity = available, ReservedQuantity = reserve, ShortageQuantity = locationShortage, CheckedAt = checkedAt, LineResultStatus = reserve > 0 ? PurchaseRequisitionLineStatuses.PartiallyReserved : PurchaseRequisitionLineStatuses.PurchaseRequired, CreatedBy = user.LoginId });
                if (reserve > 0)
                {
                    if (await db.StockReservations.AnyAsync(x => x.PurchaseRequisitionLineId == line.Id && x.LocationKey == location.LocationKey && x.Status == "Active", ct)) return Results.Conflict(new { message = $"Line {line.LineNumber}: active reservation already exists for warehouse/bin allocation." });
                    var reservation = new StockReservation { CompanyId = pr.CompanyId, PurchaseRequisitionId = pr.Id, PurchaseRequisitionLineId = line.Id, ItemId = line.ItemId.Value, WarehouseId = location.WarehouseId, RackBinId = location.RackBinId, LocationKey = location.LocationKey, ReservedQuantity = reserve, ReservationNumber = $"RSV-{pr.PrNumber}-{line.LineNumber:000}-{location.WarehouseCode}-{location.RackBinCode ?? "NA"}", ReservedBy = user.LoginId, CorrelationId = correlation, CreatedBy = user.LoginId };
                    db.StockReservations.Add(reservation);
                    db.StockReservationHistories.Add(new StockReservationHistory { CompanyId = pr.CompanyId, StockReservationId = reservation.Id, StockReservation = reservation, Action = "Create", NewStatus = "Active", Remarks = request.Remarks.Trim(), ActorLoginId = user.LoginId, CorrelationId = correlation, CreatedBy = user.LoginId });
                }
            }
            var activeReservedAfter = await ActiveReservedForLine(db, line.Id, ct) + totalReservedNow;
            var shortage = ReconciledShortage(line.RequestedQuantity, activeReservedAfter);
            if (activeReservedAfter > line.RequestedQuantity) return Results.Conflict(new { message = $"Line {line.LineNumber}: reservation exceeds requested quantity." });
            line.OnHandSnapshot = totalOnHand;
            line.ActiveReservedSnapshot = totalPriorReserved;
            line.AvailableSnapshot = totalAvailable;
            line.StockCheckedAt = checkedAt;
            line.ReservedQuantity = Math.Min(activeReservedAfter, line.RequestedQuantity);
            line.ShortageQuantity = shortage;
            line.ProcurementHandoffQuantity = shortage;
            line.LineStatus = shortage == 0 ? PurchaseRequisitionLineStatuses.FullyReserved : line.ReservedQuantity > 0 ? PurchaseRequisitionLineStatuses.PartiallyReserved : PurchaseRequisitionLineStatuses.PurchaseRequired;
            if (shortage > 0 && !await db.PurchaseRequirementHandoffs.AnyAsync(x => x.PurchaseRequisitionLineId == line.Id && x.Status == "PendingRFQ", ct))
            {
                var handoffLocation = locations[0];
                db.PurchaseRequirementHandoffs.Add(new PurchaseRequirementHandoff { CompanyId = pr.CompanyId, PurchaseRequisitionId = pr.Id, PurchaseRequisitionLineId = line.Id, ItemId = line.ItemId.Value, WarehouseId = handoffLocation.WarehouseId, RackBinId = handoffLocation.RackBinId, LocationKey = handoffLocation.LocationKey, HandoffQuantity = shortage, HandoffNumber = $"PHO-{pr.PrNumber}-{line.LineNumber:000}", HandoffBy = user.LoginId, CorrelationId = correlation, CreatedBy = user.LoginId });
            }
            anyReservation |= line.ReservedQuantity > 0;
            anyShortage |= shortage > 0;
        }
        check.ResultStatus = anyShortage ? anyReservation ? PurchaseRequisitionStatuses.PartiallyAvailable : PurchaseRequisitionStatuses.NotAvailable : PurchaseRequisitionStatuses.FullyAvailable;
        db.StockAvailabilityChecks.Add(check);
        SetStatus(db, pr, check.ResultStatus, request.Remarks, user, correlation);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        await audit.WriteAsync("Stores", "StockCheck", nameof(PurchaseRequisition), pr.Id.ToString(), null, new { pr.PrNumber, check.ResultStatus }, ct);
        return Results.Ok(new { check.CheckNumber, check.ResultStatus, pr.PrNumber });
    }

    private static async Task<List<StockLocation>> ResolveLocations(PurchaseRequisitionLine line, PurchaseRequisition pr, StockCheckRequest request, NexaErpDbContext db, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var requested = request.Locations?.Where(x => x.LineNumber == line.LineNumber).ToList() ?? [];
        if (requested.Count == 0)
        {
            var warehouseId = line.PreferredWarehouseId ?? pr.DeliveryWarehouseId;
            if (!warehouseId.HasValue) return [];
            return await db.WarehouseConditionLocations.AsNoTracking()
                .Where(x => x.OrganizationId == pr.OrganizationId && x.WarehouseId == warehouseId.Value && x.IsActive && x.ConditionCode == InventoryConditionCodes.Available && x.EffectiveFrom <= today && (!x.EffectiveTo.HasValue || x.EffectiveTo.Value >= today) && x.RackBin!.IsActive && x.Warehouse!.IsActive)
                .Select(x => new StockLocation(x.WarehouseId, x.Warehouse!.WarehouseCode, x.RackBinId, x.RackBin!.BinCode, StoreLocationKey.Derive(x.WarehouseId, x.RackBinId)))
                .ToListAsync(ct);
        }
        var result = new List<StockLocation>();
        foreach (var input in requested)
        {
            if (string.IsNullOrWhiteSpace(input.RackBinCode)) throw new InvalidOperationException($"Line {line.LineNumber}: a physical Rack/Bin is required.");
            var warehouseCode = MasterEndpointHelpers.NormalizeCode(input.WarehouseCode);
            var rackBinCode = MasterEndpointHelpers.NormalizeCode(input.RackBinCode);
            var mapping = await db.WarehouseConditionLocations.AsNoTracking()
                .Include(x => x.Warehouse).Include(x => x.RackBin)
                .SingleOrDefaultAsync(x => x.OrganizationId == pr.OrganizationId && x.Warehouse!.WarehouseCode == warehouseCode && x.Warehouse.IsActive && x.RackBin!.BinCode == rackBinCode && x.RackBin.IsActive && x.IsActive && x.ConditionCode == InventoryConditionCodes.Available && x.EffectiveFrom <= today && (!x.EffectiveTo.HasValue || x.EffectiveTo.Value >= today), ct)
                ?? throw new InvalidOperationException($"Line {line.LineNumber}: active AVAILABLE warehouse/RackBin condition mapping was not found.");
            var key = LocationKey(mapping.WarehouseId, mapping.RackBinId);
            if (result.Any(x => x.LocationKey == key)) throw new InvalidOperationException($"Line {line.LineNumber}: duplicate warehouse/bin allocation is not allowed.");
            result.Add(new StockLocation(mapping.WarehouseId, mapping.Warehouse!.WarehouseCode, mapping.RackBinId, mapping.RackBin!.BinCode, key));
        }
        return result;
    }

    private static async Task<IResult> Reservations(NexaErpDbContext db, ICurrentUser user, string? reservationNumber, int? page, int? pageSize, CancellationToken ct)
    {
        var p = MasterEndpointHelpers.NormalizePaging(page, pageSize);
        var allowedPrIds = Scope(db.PurchaseRequisitions.AsNoTracking(), user, db).Select(x => x.Id);
        var q = db.StockReservations.AsNoTracking().Where(x => allowedPrIds.Contains(x.PurchaseRequisitionId));if(!string.IsNullOrWhiteSpace(reservationNumber)){var number=reservationNumber.Trim().ToUpperInvariant();q=q.Where(x=>x.ReservationNumber==number);}var ordered=q.Include(x => x.PurchaseRequisition).Include(x => x.PurchaseRequisitionLine).Include(x => x.Warehouse).Include(x => x.RackBin).OrderBy(x => x.ReservationNumber);
        var total = await q.CountAsync(ct);
        var rows = await ordered.Skip(p.Skip).Take(p.PageSize).Select(x => new StockReservationSummary(x.Id, x.ReservationNumber, x.PurchaseRequisition!.PrNumber, x.PurchaseRequisitionLine!.LineNumber, x.PurchaseRequisitionLine.ItemCodeSnapshot, x.Warehouse!.WarehouseCode, x.RackBin == null ? null : x.RackBin.BinCode, x.ReservedQuantity, x.Status)).ToListAsync(ct);
        return Results.Ok(new PagedResponse<StockReservationSummary>(total, p.PageNumber, p.PageSize, rows));
    }

    private static async Task<IResult> Handoffs(NexaErpDbContext db, ICurrentUser user, string? handoffNumber, int? page, int? pageSize, CancellationToken ct)
    {
        var p = MasterEndpointHelpers.NormalizePaging(page, pageSize);
        var allowedPrIds = Scope(db.PurchaseRequisitions.AsNoTracking(), user, db).Select(x => x.Id);
        var q = db.PurchaseRequirementHandoffs.AsNoTracking().Where(x => allowedPrIds.Contains(x.PurchaseRequisitionId));if(!string.IsNullOrWhiteSpace(handoffNumber)){var number=handoffNumber.Trim().ToUpperInvariant();q=q.Where(x=>x.HandoffNumber==number);}var ordered=q.Include(x => x.PurchaseRequisition).Include(x => x.PurchaseRequisitionLine).Include(x => x.Warehouse).Include(x => x.RackBin).OrderBy(x => x.HandoffNumber);
        var total = await q.CountAsync(ct);
        var rows = await ordered.Skip(p.Skip).Take(p.PageSize).Select(x => new PurchaseRequirementHandoffSummary(x.Id, x.HandoffNumber, x.PurchaseRequisition!.PrNumber, x.PurchaseRequisitionLine!.LineNumber, x.PurchaseRequisitionLine.ItemCodeSnapshot, x.Warehouse!.WarehouseCode, x.RackBin == null ? null : x.RackBin.BinCode, x.HandoffQuantity, x.Status)).ToListAsync(ct);
        return Results.Ok(new PagedResponse<PurchaseRequirementHandoffSummary>(total, p.PageNumber, p.PageSize, rows));
    }
}
