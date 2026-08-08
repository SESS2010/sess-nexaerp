using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Application.Audit;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Inventory;
using SESS.NexaERP.Application.Purchase;
using SESS.NexaERP.Domain.Purchase;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Api.Endpoints;

public static partial class PurchaseRequisitionEndpoints
{
    private static async Task<IResult> StockCheck(string prNumber, StockCheckRequest request, NexaErpDbContext db, ICurrentUser user, IAuditWriter audit, CancellationToken ct)
    {
        var pr = await IncludeDetail(db.PurchaseRequisitions).SingleOrDefaultAsync(x => x.PrNumber == NormalizePr(prNumber), ct);
        if (pr is null) return Results.NotFound(new { message = "Purchase requisition not found." });
        if (pr.Status != PurchaseRequisitionStatuses.StockCheckPending) return Results.Conflict(new { message = "Stock check is allowed only after PR approval." });
        if (request.Version != pr.Version) return Results.Conflict(new { message = "Stale record version. Refresh and retry." });
        if (string.IsNullOrWhiteSpace(request.Remarks)) return Results.BadRequest(new { message = "Remarks are required." });
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var correlation = string.IsNullOrWhiteSpace(request.IdempotencyKey) ? Correlation("STOCKCHECK") : request.IdempotencyKey.Trim();
        var existingCheck = await db.StockAvailabilityChecks.SingleOrDefaultAsync(x => x.PurchaseRequisitionId == pr.Id && x.CorrelationId == correlation, ct);
        if (existingCheck is not null) return Results.Ok(new { existingCheck.CheckNumber, existingCheck.ResultStatus });
        var check = new StockAvailabilityCheck { PurchaseRequisitionId = pr.Id, CheckNumber = $"SC-{pr.PrNumber}", CheckedBy = user.LoginId, Remarks = request.Remarks.Trim(), CorrelationId = correlation, CreatedBy = user.LoginId };
        var anyShortage = false;
        var anyReservation = false;
        foreach (var line in pr.Lines.OrderBy(x => x.LineNumber))
        {
            if (line.ItemId is null) return Results.BadRequest(new { message = $"Line {line.LineNumber} requires controlled New Item Request before PR stock check." });
            var warehouseId = line.PreferredWarehouseId ?? pr.DeliveryWarehouseId;
            var onHand = await OnHand(db, line.ItemId.Value, warehouseId, ct);
            var activeReserved = await ActiveReserved(db, line.ItemId.Value, warehouseId, ct);
            var available = Math.Max(onHand - activeReserved, 0);
            var reserve = Math.Min(line.RequestedQuantity, available);
            var shortage = Math.Max(line.RequestedQuantity - reserve, 0);
            line.OnHandSnapshot = onHand;
            line.ActiveReservedSnapshot = activeReserved;
            line.AvailableSnapshot = available;
            line.StockCheckedAt = DateTimeOffset.UtcNow;
            line.ReservedQuantity = reserve;
            line.ShortageQuantity = shortage;
            line.ProcurementHandoffQuantity = shortage;
            line.LineStatus = shortage == 0 ? PurchaseRequisitionLineStatuses.FullyReserved : reserve > 0 ? PurchaseRequisitionLineStatuses.PartiallyReserved : PurchaseRequisitionLineStatuses.PurchaseRequired;
            check.Lines.Add(new StockAvailabilityCheckLine { PurchaseRequisitionLineId = line.Id, ItemId = line.ItemId.Value, WarehouseId = warehouseId, RequestedQuantity = line.RequestedQuantity, OnHandQuantity = onHand, ActiveReservedQuantity = activeReserved, AvailableQuantity = available, ReservedQuantity = reserve, ShortageQuantity = shortage, LineResultStatus = line.LineStatus, CreatedBy = user.LoginId });
            if (reserve > 0 && !await db.StockReservations.AnyAsync(x => x.PurchaseRequisitionLineId == line.Id && x.Status == "Active", ct))
            {
                var reservation = new StockReservation { PurchaseRequisitionId = pr.Id, PurchaseRequisitionLineId = line.Id, ItemId = line.ItemId.Value, WarehouseId = warehouseId, ReservedQuantity = reserve, ReservationNumber = $"RSV-{pr.PrNumber}-{line.LineNumber:000}", ReservedBy = user.LoginId, CorrelationId = correlation, CreatedBy = user.LoginId };
                db.StockReservations.Add(reservation);
                db.StockReservationHistories.Add(new StockReservationHistory { StockReservationId = reservation.Id, Action = "Create", NewStatus = "Active", Remarks = request.Remarks.Trim(), ActorLoginId = user.LoginId, CorrelationId = correlation, CreatedBy = user.LoginId });
            }
            if (shortage > 0 && !await db.PurchaseRequirementHandoffs.AnyAsync(x => x.PurchaseRequisitionLineId == line.Id && x.Status == "PendingRFQ", ct))
            {
                db.PurchaseRequirementHandoffs.Add(new PurchaseRequirementHandoff { PurchaseRequisitionId = pr.Id, PurchaseRequisitionLineId = line.Id, ItemId = line.ItemId.Value, WarehouseId = warehouseId, HandoffQuantity = shortage, HandoffNumber = $"PHO-{pr.PrNumber}-{line.LineNumber:000}", HandoffBy = user.LoginId, CorrelationId = correlation, CreatedBy = user.LoginId });
            }
            anyReservation |= reserve > 0;
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

    private static async Task<IResult> Reservations(NexaErpDbContext db, int? page, int? pageSize, CancellationToken ct)
    {
        var p = MasterEndpointHelpers.NormalizePaging(page, pageSize);
        var q = db.StockReservations.AsNoTracking().Include(x => x.PurchaseRequisition).Include(x => x.PurchaseRequisitionLine).OrderBy(x => x.ReservationNumber);
        var total = await q.CountAsync(ct);
        var rows = await q.Skip(p.Skip).Take(p.PageSize).Select(x => new StockReservationSummary(x.Id, x.ReservationNumber, x.PurchaseRequisition!.PrNumber, x.PurchaseRequisitionLine!.LineNumber, x.PurchaseRequisitionLine.ItemCodeSnapshot, x.ReservedQuantity, x.Status)).ToListAsync(ct);
        return Results.Ok(new PagedResponse<StockReservationSummary>(total, p.PageNumber, p.PageSize, rows));
    }

    private static async Task<IResult> Handoffs(NexaErpDbContext db, int? page, int? pageSize, CancellationToken ct)
    {
        var p = MasterEndpointHelpers.NormalizePaging(page, pageSize);
        var q = db.PurchaseRequirementHandoffs.AsNoTracking().Include(x => x.PurchaseRequisition).Include(x => x.PurchaseRequisitionLine).OrderBy(x => x.HandoffNumber);
        var total = await q.CountAsync(ct);
        var rows = await q.Skip(p.Skip).Take(p.PageSize).Select(x => new PurchaseRequirementHandoffSummary(x.Id, x.HandoffNumber, x.PurchaseRequisition!.PrNumber, x.PurchaseRequisitionLine!.LineNumber, x.PurchaseRequisitionLine.ItemCodeSnapshot, x.HandoffQuantity, x.Status)).ToListAsync(ct);
        return Results.Ok(new PagedResponse<PurchaseRequirementHandoffSummary>(total, p.PageNumber, p.PageSize, rows));
    }
}
