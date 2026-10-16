using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Api.Security;
using SESS.NexaERP.Application.Authorization;
using SESS.NexaERP.Application.Common;
using SESS.NexaERP.Application.Stores;
using SESS.NexaERP.Infrastructure.Persistence;
using Candidate = SESS.NexaERP.Api.Endpoints.GoodsReceiptBarcodeEndpoints.Candidate;

namespace SESS.NexaERP.Api.Endpoints;
public static class MaterialBarcodeEndpoints
{
    public sealed record RequestItem(Guid ItemId, string ItemCode, string Name, string Uom, Guid? BaseUomId,
        string? Barcode, Guid? InventorySerialId, string? StoredSerialNumber);
    public static IEndpointRouteBuilder MapMaterialBarcodeEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/stores/barcodes").WithMetadata(new BarcodeBusinessDate()).RequireAuthorization()
            .AddEndpointFilter(EmployeeScopeEndpointFilter.RequireResolvedEmployeeAndScope);
        group.MapGet("/request-items", async (string scan, NexaErpDbContext db, ICurrentUser user, HttpContext context, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(scan) || scan.Length > 200 || scan.Any(char.IsControl))
                return Results.BadRequest(new { message = "Scan a non-empty text barcode, item code or serial (maximum 200 characters)." });
            var token = scan.Trim().ToUpperInvariant();
            var scopes = BarcodeScope.Active(db, user, context);
            var companyId = await db.Companies.Where(c => c.Code == user.OrganizationId && c.IsActive &&
                scopes.Any(s => s.CompanyId == c.Id)).Select(c => (Guid?)c.Id).SingleOrDefaultAsync(ct);
            if (!companyId.HasValue) return Results.Forbid();
            var isLot = GrnLotBarcode.TryParse(token, out var lotCompany, out var lotNumber, out var lotLine, out var lotOrdinal) && lotCompany == user.OrganizationId;
            var rows = await db.Items.AsNoTracking().Where(i => i.IsActive && i.ApprovalStatus == "Approved" &&
                (i.ItemCode.ToUpper() == token || i.Barcode != null && i.Barcode.ToUpper() == token ||
                    isLot && db.GoodsReceiptLineLotAllocations.Any(a => a.CompanyId == companyId.Value && a.LotOrdinal == lotOrdinal &&
                        a.GoodsReceiptLine != null && a.GoodsReceiptLine.ItemId == i.Id && a.GoodsReceiptLine.LineNumber == lotLine &&
                        a.GoodsReceiptLine.GoodsReceipt != null && a.GoodsReceiptLine.GoodsReceipt.GrnNumber == lotNumber && a.GoodsReceiptLine.GoodsReceipt.CompanyId == companyId.Value) ||
                    db.InventorySerials.Any(serial => serial.CompanyId == companyId.Value && serial.ItemId == i.Id && serial.StoredSerialNumber.ToUpper() == token)))
                .Select(i => new RequestItem(i.Id, i.ItemCode, i.Name, i.Uom, i.BaseUomId, i.Barcode,
                    db.InventorySerials.Where(serial => serial.CompanyId == companyId.Value && serial.ItemId == i.Id && serial.StoredSerialNumber.ToUpper() == token)
                        .Select(serial => (Guid?)serial.Id).FirstOrDefault(),
                    db.InventorySerials.Where(serial => serial.CompanyId == companyId.Value && serial.ItemId == i.Id && serial.StoredSerialNumber.ToUpper() == token)
                        .Select(serial => serial.StoredSerialNumber).FirstOrDefault())).ToListAsync(ct);
            return Results.Ok(rows);
        }).RequirePagePermission("stores.material-issue-requests", PagePermissionActions.Create);
        group.MapGet("/issue-requests/{requestId:guid}", async (Guid requestId, NexaErpDbContext db,
            ICurrentUser user, IMaterialIssueService service, HttpContext context, CancellationToken ct) =>
        {
            if (!BarcodeScope.RequireStores(user, "issue")) return Results.Forbid();
            var scopes = BarcodeScope.Active(db, user, context);
            var request = await db.MaterialIssueRequests.AsNoTracking().Where(r => r.Id == requestId &&
                db.Companies.Any(c => c.Id == r.CompanyId && c.Code == user.OrganizationId && c.IsActive) &&
                scopes.Any(s => s.CompanyId == r.CompanyId && (!s.OwnRecordsOnly || r.RequestedByEmployeeId == user.EmployeeId)) &&
                (r.Status == "APPROVED" || r.Status == "PARTIALLY_FULFILLED"))
                .Select(r => new { r.Id, r.CompanyId }).SingleOrDefaultAsync(ct);
            if (request is null) return Results.NotFound(new { message = "Approved MIR not found in the current issue scope." });
            var lines = await db.MaterialIssueRequestLines.AsNoTracking().Where(l => l.MaterialIssueRequestId == request.Id && l.CompanyId == request.CompanyId)
                .Select(l => new { l.Id, l.ItemId, l.Item!.ItemCode, l.Item.Barcode, l.Item.SerialNumberTracking }).ToListAsync(ct);
            // One request-local active-scope snapshot avoids a database round trip for every physical serial.
            // The company/warehouse/rack predicates are unchanged; command authorization is still checked at Issue.
            var serialScopes = await scopes.Where(scope => scope.CompanyId == request.CompanyId)
                .Select(scope => new { scope.WarehouseId, scope.RackBinId }).ToListAsync(ct);
            var result = new List<Candidate>();
            foreach (var line in lines)
            {
                var serials = new List<AvailableMaterialIssueSerialView>();
                foreach (var serial in await service.AvailableSerialsAsync(line.Id, ct))
                    if (serial.AvailableQuantity >= 1 && serialScopes.Any(scope =>
                        (!scope.WarehouseId.HasValue || scope.WarehouseId == serial.WarehouseId) &&
                        (!scope.RackBinId.HasValue || scope.RackBinId == serial.RackBinId))) serials.Add(serial);
                var requiresSerial = line.SerialNumberTracking || serials.Count > 0;
                result.Add(new(line.Id.ToString(), line.ItemId, line.ItemCode, line.Barcode, null, null, requiresSerial));
                foreach (var serial in serials)
                    result.Add(new(line.Id.ToString(), line.ItemId, line.ItemCode, line.Barcode, serial.InventorySerialId, serial.StoredSerialNumber));
            }
            result.AddRange(await BarcodeStockLotCandidates.Read(db, user, request.CompanyId, result.ToArray(), null, ct, BarcodeBusinessDate.ForRequest(context)));
            return Results.Ok(result);
        }).RequirePagePermission("stores.material-issues", PagePermissionActions.Issue);

        group.MapGet("/return-issues/{issueId:guid}", async (Guid issueId, NexaErpDbContext db, ICurrentUser user, HttpContext context, CancellationToken ct) =>
        {
            var scopes = BarcodeScope.Active(db, user, context);
            var issue = await db.MaterialIssues.AsNoTracking().Where(i => i.Id == issueId && i.IssuedToEmployeeId == user.EmployeeId &&
                db.Companies.Any(c => c.Id == i.CompanyId && c.Code == user.OrganizationId && c.IsActive) &&
                scopes.Any(s => s.CompanyId == i.CompanyId)).Select(i => new { i.Id, i.CompanyId }).SingleOrDefaultAsync(ct);
            if (issue is null) return Results.NotFound(new { message = "Own material issue not found in the selected company." });
            var rows = await db.MaterialIssueLines.AsNoTracking().Where(l => l.MaterialIssueId == issue.Id && l.CompanyId == issue.CompanyId)
                .Select(l => new Candidate(l.Id.ToString(), l.ItemId, l.Item!.ItemCode, l.Item.Barcode,
                    l.InventorySerialId, db.InventorySerials.Where(serial => serial.Id == l.InventorySerialId && serial.CompanyId == issue.CompanyId).Select(serial => serial.StoredSerialNumber).FirstOrDefault())).ToListAsync(ct);
            var lots = await db.MaterialIssueLines.AsNoTracking().Where(line => line.MaterialIssueId == issue.Id && line.CompanyId == issue.CompanyId && line.GoodsReceiptLineLotAllocationId.HasValue)
                .Join(db.GoodsReceiptLineLotAllocations.Where(allocation => allocation.CompanyId == issue.CompanyId && allocation.GoodsReceiptLine != null && allocation.GoodsReceiptLine.CompanyId == issue.CompanyId && allocation.GoodsReceiptLine.GoodsReceipt != null && allocation.GoodsReceiptLine.GoodsReceipt.CompanyId == issue.CompanyId),
                    line => line.GoodsReceiptLineLotAllocationId!.Value, allocation => allocation.Id,
                    (line, allocation) => new { LineId = line.Id, IssueLotId = line.InventoryLotId, allocation.LotOrdinal,
                        AllocationLotId = allocation.InventoryLotId, allocation.GoodsReceiptLine!.LineNumber, allocation.GoodsReceiptLine.GoodsReceipt!.GrnNumber })
                .ToListAsync(ct);
            foreach (var lot in lots.Where(lot => lot.IssueLotId == lot.AllocationLotId))
            {
                var original = rows.Single(row => row.LineKey == lot.LineId.ToString());
                rows.Add(original with { InventorySerialId = null, StoredSerialNumber = null,
                    Barcode = GrnLotBarcode.Create(user.OrganizationId!, lot.GrnNumber, lot.LineNumber, lot.LotOrdinal),
                    RequiresSerial = original.InventorySerialId.HasValue });
            }
            rows.AddRange(rows.Where(row => row.InventorySerialId.HasValue)
                .Select(row => row with { InventorySerialId = null, StoredSerialNumber = null, RequiresSerial = true }).ToArray());
            return Results.Ok(rows);
        }).RequirePagePermission("stores.material-returns", PagePermissionActions.Create);
        endpoints.MapRackBarcodeLabelEndpoints();
        return endpoints;
    }
}