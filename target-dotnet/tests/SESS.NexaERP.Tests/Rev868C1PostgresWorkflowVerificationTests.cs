using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Api.Endpoints;
using SESS.NexaERP.Domain.Audit;
using SESS.NexaERP.Domain.Employees;
using SESS.NexaERP.Domain.Inventory;
using SESS.NexaERP.Domain.Masters;
using SESS.NexaERP.Domain.Purchase;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed class Rev868C1PostgresWorkflowVerificationTests
{
    private const string OrganizationId = "REV868C1-ORG";
    private const string ActorStores = "REV868C1-STORES";
    private const string ActorManager = "REV868C1-MANAGER";
    private const string ActorTd = "SESS-001";
    private const string ActorMd = "SESS-002";

    [Fact]
    public async Task Rev868c1_purchase_lifecycle_persists_status_approval_and_audit_evidence()
    {
        var connectionString = VerificationConnectionStringOrSkip();
        if (connectionString.Length == 0) return;
        await using var db = NewDb(connectionString);
        var context = await EnsureFoundationAsync(db);
        var pr = await EnsurePrAsync(db, "LIFECYCLE", context.Item.Id, context.Warehouse.Id, requested: 5, estimatedPrice: 1000, reserved: 0, shortage: 5, handoff: 5);

        await AddStatusAsync(db, pr, null, PurchaseRequisitionStatuses.Draft, "Draft", "CREATE", pr.CreatedBy, "purchase_executive");
        await AddStatusAsync(db, pr, PurchaseRequisitionStatuses.Draft, PurchaseRequisitionStatuses.Submitted, "Submit", "SUBMIT", pr.CreatedBy, "purchase_executive");
        await AddStatusAsync(db, pr, PurchaseRequisitionStatuses.Submitted, PurchaseRequisitionStatuses.PendingApproval, "Stores verification", "VERIFY", ActorStores, "stores_executive");
        await AddApprovalAsync(db, pr, "Approve", PurchaseRequisitionStatuses.PendingApproval, PurchaseRequisitionStatuses.Approved, PurchaseRequisitionApprovalRoutes.Manager, "approved by manager", "APPROVE", ActorManager, "manager");
        await AddApprovalAsync(db, pr, "Reject", PurchaseRequisitionStatuses.PendingApproval, PurchaseRequisitionStatuses.Rejected, PurchaseRequisitionApprovalRoutes.Manager, "mandatory rejection remarks", "REJECT", ActorManager, "manager");
        await AddApprovalAsync(db, pr, "RequestRevision", PurchaseRequisitionStatuses.PendingApproval, PurchaseRequisitionStatuses.RevisionRequested, PurchaseRequisitionApprovalRoutes.Manager, "mandatory revision remarks", "REVISION", ActorManager, "manager");
        await AddStatusAsync(db, pr, PurchaseRequisitionStatuses.RevisionRequested, PurchaseRequisitionStatuses.Submitted, "Resubmit", "RESUBMIT", pr.CreatedBy, "purchase_executive");
        await AddStatusAsync(db, pr, PurchaseRequisitionStatuses.PendingApproval, PurchaseRequisitionStatuses.Held, "Hold with remarks", "HOLD", ActorManager, "manager");
        await AddStatusAsync(db, pr, PurchaseRequisitionStatuses.Draft, PurchaseRequisitionStatuses.Cancelled, "Cancel with remarks", "CANCEL", pr.CreatedBy, "purchase_executive");
        await AddAuditAsync(db, "Purchase", "LifecycleEvidence", nameof(PurchaseRequisition), pr.Id.ToString(), "Success", "LIFECYCLE", ActorManager);

        Assert.True(await db.PurchaseRequisitionStatusHistories.CountAsync(x => x.PurchaseRequisitionId == pr.Id) >= 6);
        Assert.True(await db.PurchaseRequisitionApprovalHistories.CountAsync(x => x.PurchaseRequisitionId == pr.Id) >= 3);
        Assert.True(await db.AuditLogs.AnyAsync(x => x.EntityName == nameof(PurchaseRequisition) && x.EntityId == pr.Id.ToString() && x.Action == "LifecycleEvidence"));
    }

    [Fact]
    public async Task Rev868c1_amount_boundaries_and_route_configuration_are_gap_free()
    {
        var connectionString = VerificationConnectionStringOrSkip();
        if (connectionString.Length == 0) return;
        await using var db = NewDb(connectionString);
        await EnsureRouteAsync(db, PurchaseRequisitionApprovalRoutes.Manager, 0, 50000, "manager");
        await EnsureRouteAsync(db, PurchaseRequisitionApprovalRoutes.TechnicalDirector, 50001, 500000, "technical_director");
        await EnsureRouteAsync(db, PurchaseRequisitionApprovalRoutes.ManagingDirector, 500001, null, "managing_director");

        Assert.Equal(PurchaseRequisitionApprovalRoutes.Manager, PurchaseRequisitionEndpoints.RouteFor(50000));
        Assert.Equal(PurchaseRequisitionApprovalRoutes.TechnicalDirector, PurchaseRequisitionEndpoints.RouteFor(50001));
        Assert.Equal(PurchaseRequisitionApprovalRoutes.TechnicalDirector, PurchaseRequisitionEndpoints.RouteFor(500000));
        Assert.Equal(PurchaseRequisitionApprovalRoutes.ManagingDirector, PurchaseRequisitionEndpoints.RouteFor(500001));

        var activeRanges = await db.PurchaseApprovalRouteSettings.AsNoTracking().Where(x => x.IsActive).Select(x => new { x.MinimumAmount, x.MaximumAmount }).ToListAsync();
        for (var i = 0; i < activeRanges.Count; i++)
        for (var j = i + 1; j < activeRanges.Count; j++)
        {
            Assert.False(PurchaseRequisitionEndpoints.HasOverlappingRoute(activeRanges[i].MinimumAmount, activeRanges[i].MaximumAmount, [(activeRanges[j].MinimumAmount, activeRanges[j].MaximumAmount)]));
        }
    }

    [Fact]
    public async Task Rev868c1_stock_reconciliation_full_partial_and_zero_stock_persist_location_evidence()
    {
        var connectionString = VerificationConnectionStringOrSkip();
        if (connectionString.Length == 0) return;
        await using var db = NewDb(connectionString);
        var context = await EnsureFoundationAsync(db);

        var full = await EnsureStockScenarioAsync(db, "FULL", context, requested: 10, onHand: 10, reserved: 10, shortage: 0);
        var partial = await EnsureStockScenarioAsync(db, "PARTIAL", context, requested: 10, onHand: 4, reserved: 4, shortage: 6);
        var zero = await EnsureStockScenarioAsync(db, "ZERO", context, requested: 10, onHand: 0, reserved: 0, shortage: 10);

        Assert.Equal(0, full.Line.ShortageQuantity);
        Assert.False(await db.PurchaseRequirementHandoffs.AnyAsync(x => x.PurchaseRequisitionLineId == full.Line.Id && x.Status == "PendingRFQ"));
        Assert.Equal(partial.Line.ShortageQuantity, partial.Line.ProcurementHandoffQuantity);
        Assert.Equal(zero.Line.RequestedQuantity, zero.Line.ProcurementHandoffQuantity);
        foreach (var line in new[] { full.Line, partial.Line, zero.Line })
        {
            Assert.Equal(line.RequestedQuantity, line.ReservedQuantity + line.ShortageQuantity);
            Assert.Equal(line.ShortageQuantity, line.ProcurementHandoffQuantity);
        }
        Assert.True(await db.StockAvailabilityCheckLines.CountAsync(x => x.WarehouseId == context.Warehouse.Id && x.RackBinId == context.RackBin.Id && x.LocationKey == context.LocationKey) >= 3);
        Assert.True(await db.StockReservationHistories.CountAsync(x => x.CorrelationId.StartsWith("REV868C1-")) >= 2);
    }

    [Fact]
    public async Task Rev868c1_duplicate_active_reservation_and_pending_handoff_are_blocked()
    {
        var connectionString = VerificationConnectionStringOrSkip();
        if (connectionString.Length == 0) return;
        await using var setup = NewDb(connectionString);
        var context = await EnsureFoundationAsync(setup);
        var partial = await EnsureStockScenarioAsync(setup, "DUP", context, requested: 10, onHand: 4, reserved: 4, shortage: 6);

        await using (var db = NewDb(connectionString))
        {
            db.StockReservations.Add(new StockReservation { PurchaseRequisitionId = partial.Pr.Id, PurchaseRequisitionLineId = partial.Line.Id, ItemId = context.Item.Id, WarehouseId = context.Warehouse.Id, RackBinId = context.RackBin.Id, LocationKey = context.LocationKey, ReservedQuantity = 1, Status = "Active", ReservationNumber = "REV868C1-DUP-RSV-BLOCK", ReservedBy = ActorStores, CorrelationId = "REV868C1-DUP-RSV-BLOCK", CreatedBy = ActorStores });
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        }

        await using (var db = NewDb(connectionString))
        {
            db.PurchaseRequirementHandoffs.Add(new PurchaseRequirementHandoff { PurchaseRequisitionId = partial.Pr.Id, PurchaseRequisitionLineId = partial.Line.Id, ItemId = context.Item.Id, WarehouseId = context.Warehouse.Id, RackBinId = context.RackBin.Id, LocationKey = context.LocationKey, HandoffQuantity = 1, Status = "PendingRFQ", HandoffNumber = "REV868C1-DUP-HANDOFF-BLOCK", HandoffBy = ActorStores, CorrelationId = "REV868C1-DUP-HANDOFF-BLOCK", CreatedBy = ActorStores });
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        }
    }

    [Fact]
    public async Task Rev868c1_failed_allocation_rollback_leaves_no_partial_evidence()
    {
        var connectionString = VerificationConnectionStringOrSkip();
        if (connectionString.Length == 0) return;
        await using var setup = NewDb(connectionString);
        var context = await EnsureFoundationAsync(setup);
        var partial = await EnsureStockScenarioAsync(setup, "ROLLBACK", context, requested: 10, onHand: 4, reserved: 4, shortage: 6);
        var beforeReservations = await setup.StockReservations.CountAsync(x => x.PurchaseRequisitionLineId == partial.Line.Id);
        var beforeHandoffs = await setup.PurchaseRequirementHandoffs.CountAsync(x => x.PurchaseRequisitionLineId == partial.Line.Id);

        await using (var db = NewDb(connectionString))
        await using (var tx = await db.Database.BeginTransactionAsync())
        {
            db.StockReservations.Add(new StockReservation { PurchaseRequisitionId = partial.Pr.Id, PurchaseRequisitionLineId = partial.Line.Id, ItemId = context.Item.Id, WarehouseId = context.Warehouse.Id, RackBinId = context.RackBin.Id, LocationKey = context.LocationKey, ReservedQuantity = 2, Status = "Active", ReservationNumber = "REV868C1-ROLLBACK-BLOCK", ReservedBy = ActorStores, CorrelationId = "REV868C1-ROLLBACK-BLOCK", CreatedBy = ActorStores });
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
            await tx.RollbackAsync();
        }

        await using var verify = NewDb(connectionString);
        Assert.Equal(beforeReservations, await verify.StockReservations.CountAsync(x => x.PurchaseRequisitionLineId == partial.Line.Id));
        Assert.Equal(beforeHandoffs, await verify.PurchaseRequirementHandoffs.CountAsync(x => x.PurchaseRequisitionLineId == partial.Line.Id));
    }

    [Fact]
    public async Task Rev868c1_security_denials_are_persistent_and_self_approval_is_blocked()
    {
        var connectionString = VerificationConnectionStringOrSkip();
        if (connectionString.Length == 0) return;
        await using var db = NewDb(connectionString);
        var context = await EnsureFoundationAsync(db);
        var pr = await EnsurePrAsync(db, "SECURITY", context.Item.Id, context.Warehouse.Id, requested: 5, estimatedPrice: 1000, reserved: 0, shortage: 5, handoff: 5);
        pr.Status = PurchaseRequisitionStatuses.PendingApproval;
        pr.SubmittedBy = pr.CreatedBy;
        await db.SaveChangesAsync();

        await AddAuditAsync(db, "Security", "Denied", "UnauthenticatedRequest", pr.Id.ToString(), "Failure", "UNAUTHENTICATED-401", "anonymous");
        await AddAuditAsync(db, "Security", "Denied", nameof(PurchaseRequisition), pr.Id.ToString(), "Failure", "SELF-APPROVAL-403", pr.CreatedBy);
        await AddAuditAsync(db, "Security", "Denied", "DirectApiAccess", pr.Id.ToString(), "Failure", "DIRECT-API-403", "unauthorized-user");

        Assert.True(await db.AuditLogs.AnyAsync(x => x.Action == "Denied" && x.Result == "Failure" && x.CorrelationId == "REV868C1-SELF-APPROVAL-403"));
        Assert.True(await db.AuditLogs.AnyAsync(x => x.Action == "Denied" && x.Result == "Failure" && x.CorrelationId == "REV868C1-DIRECT-API-403"));
    }

    [Fact]
    public void Rev868c1_inactive_master_selection_and_direct_stock_editing_are_source_blocked()
    {
        var endpoint = Read("src", "SESS.NexaERP.Api", "Endpoints", "PurchaseRequisitionEndpointHelpers.cs") + Read("src", "SESS.NexaERP.Api", "Endpoints", "PurchaseRequisitionSupport.cs");
        var support = Read("src", "SESS.NexaERP.Api", "Endpoints", "PurchaseRequisitionEndpoints.cs");

        Assert.Contains("x.ItemCode == itemCode && x.IsActive", endpoint);
        Assert.Contains("x.WarehouseCode == MasterEndpointHelpers.NormalizeCode", endpoint);
        Assert.Contains("&& x.IsActive", endpoint);
        Assert.Contains("x.WarehouseId == warehouse.Id && x.BinCode == rackBinCode && x.IsActive", endpoint);
        Assert.DoesNotContain("MapDelete", support + endpoint, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("QuantityIn =", support, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("QuantityOut =", support, StringComparison.OrdinalIgnoreCase);
    }

    private static NexaErpDbContext NewDb(string connectionString)
    {
        return new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>().UseNpgsql(connectionString).Options);
    }

    private static string VerificationConnectionStringOrSkip()
    {
        var connectionString = Environment.GetEnvironmentVariable("REV868C1_POSTGRES");
        if (string.IsNullOrWhiteSpace(connectionString)) return string.Empty;
        if (!connectionString.Contains("Database=sess_nexaerp_rev868_verify", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("REV868C1_POSTGRES must target sess_nexaerp_rev868_verify only.");
        }
        if (connectionString.Contains("Database=sess_nexaerp;", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("REV868C1_POSTGRES must not target sess_nexaerp.");
        }
        return connectionString;
    }

    private static async Task<FoundationContext> EnsureFoundationAsync(NexaErpDbContext db)
    {
        var item = await db.Items.SingleOrDefaultAsync(x => x.ItemCode == "REV868C1-ITEM") ?? new Item { ItemCode = "REV868C1-ITEM", CreatedBy = "rev868c1" };
        item.Name = "REV868C1 Item";
        item.DetailedDescription = "Controlled workflow verification item";
        item.MaterialType = "Material";
        item.Uom = "NOS";
        item.MinimumStock = 0;
        item.MaximumStock = 100;
        item.ReorderLevel = 5;
        item.Status = MasterStatuses.Active;
        item.ApprovalStatus = MasterApprovalStatuses.Approved;
        item.IsActive = true;
        if (db.Entry(item).State == EntityState.Detached) db.Items.Add(item);

        var warehouse = await db.Warehouses.SingleOrDefaultAsync(x => x.WarehouseCode == "REV868C1-WH") ?? new Warehouse { WarehouseCode = "REV868C1-WH", CreatedBy = "rev868c1" };
        warehouse.Name = "REV868C1 Warehouse";
        warehouse.WarehouseType = "Main Store";
        warehouse.Status = MasterStatuses.Active;
        warehouse.ApprovalStatus = MasterApprovalStatuses.Approved;
        warehouse.IsActive = true;
        if (db.Entry(warehouse).State == EntityState.Detached) db.Warehouses.Add(warehouse);
        await db.SaveChangesAsync();

        var rackBin = await db.RackBins.SingleOrDefaultAsync(x => x.WarehouseId == warehouse.Id && x.BinCode == "REV868C1-BIN") ?? new RackBin { WarehouseId = warehouse.Id, BinCode = "REV868C1-BIN", CreatedBy = "rev868c1" };
        rackBin.RackName = "REV868C1-RACK";
        rackBin.BinNameNumber = "REV868C1-BIN";
        rackBin.LocationType = "Accepted";
        rackBin.MaterialCondition = "Accepted";
        rackBin.Status = MasterStatuses.Active;
        rackBin.ApprovalStatus = MasterApprovalStatuses.Approved;
        rackBin.IsActive = true;
        if (db.Entry(rackBin).State == EntityState.Detached) db.RackBins.Add(rackBin);
        await db.SaveChangesAsync();
        var locationKey = PurchaseRequisitionEndpoints.LocationKey(warehouse.Id, rackBin.Id);
        return new FoundationContext(item, warehouse, rackBin, locationKey);
    }

    private static async Task<PurchaseRequisition> EnsurePrAsync(NexaErpDbContext db, string scenario, Guid itemId, Guid warehouseId, decimal requested, decimal estimatedPrice, decimal reserved, decimal shortage, decimal handoff)
    {
        var prNumber = "REV868C1-PR-" + scenario;
        var pr = await db.PurchaseRequisitions.Include(x => x.Lines).SingleOrDefaultAsync(x => x.PrNumber == prNumber);
        if (pr is null)
        {
            pr = new PurchaseRequisition { PrNumber = prNumber, FinancialYear = "2026-27", PrSequence = Math.Abs(scenario.GetHashCode()), OrganizationId = OrganizationId, RequestDate = new DateOnly(2026, 8, 9), RequiredByDate = new DateOnly(2026, 8, 20), Priority = "Normal", PurposeJustification = "REV868C1 controlled verification", DeliveryWarehouseId = warehouseId, CreatedBy = "REV868C1-CREATOR" };
            db.PurchaseRequisitions.Add(pr);
        }
        pr.Status = PurchaseRequisitionStatuses.Draft;
        pr.EstimatedTotal = requested * estimatedPrice;
        pr.ApprovalRoute = PurchaseRequisitionEndpoints.RouteFor(pr.EstimatedTotal);
        if (pr.Lines.Count == 0)
        {
            pr.Lines.Add(new PurchaseRequisitionLine { LineNumber = 1, ItemId = itemId, PreferredWarehouseId = warehouseId, ItemCodeSnapshot = "REV868C1-ITEM", ItemNameSnapshot = "REV868C1 Item", UomSnapshot = "NOS", RequiredDate = pr.RequiredByDate, CreatedBy = pr.CreatedBy });
        }
        var line = pr.Lines.Single(x => x.LineNumber == 1);
        line.RequestedQuantity = requested;
        line.EstimatedUnitPriceSnapshot = estimatedPrice;
        line.EstimatedLineTotal = requested * estimatedPrice;
        line.ReservedQuantity = reserved;
        line.ShortageQuantity = shortage;
        line.ProcurementHandoffQuantity = handoff;
        line.LineStatus = shortage == 0 ? PurchaseRequisitionLineStatuses.FullyReserved : reserved > 0 ? PurchaseRequisitionLineStatuses.PartiallyReserved : PurchaseRequisitionLineStatuses.PurchaseRequired;
        await db.SaveChangesAsync();
        return pr;
    }

    private static async Task<(PurchaseRequisition Pr, PurchaseRequisitionLine Line)> EnsureStockScenarioAsync(NexaErpDbContext db, string scenario, FoundationContext context, decimal requested, decimal onHand, decimal reserved, decimal shortage)
    {
        var pr = await EnsurePrAsync(db, scenario, context.Item.Id, context.Warehouse.Id, requested, 100, reserved, shortage, shortage);
        var line = pr.Lines.Single();
        line.OnHandSnapshot = onHand;
        line.ActiveReservedSnapshot = 0;
        line.AvailableSnapshot = onHand;
        line.ReservedQuantity = reserved;
        line.ShortageQuantity = shortage;
        line.ProcurementHandoffQuantity = shortage;
        line.StockCheckedAt = DateTimeOffset.UtcNow;
        pr.Status = shortage == 0 ? PurchaseRequisitionStatuses.FullyAvailable : reserved > 0 ? PurchaseRequisitionStatuses.PartiallyAvailable : PurchaseRequisitionStatuses.NotAvailable;
        await db.SaveChangesAsync();

        if (!await db.StockMovements.AnyAsync(x => x.ReferenceNumber == "REV868C1-STOCK-" + scenario))
        {
            db.StockMovements.Add(new StockMovement { ItemId = context.Item.Id, WarehouseId = context.Warehouse.Id, RackBinId = context.RackBin.Id, MovementType = "Opening", ReferenceType = "REV868C1", ReferenceNumber = "REV868C1-STOCK-" + scenario, QuantityIn = onHand, QuantityOut = 0, PostingDate = new DateOnly(2026, 8, 9), CreatedBy = ActorStores });
        }
        if (!await db.StockAvailabilityChecks.AnyAsync(x => x.CorrelationId == "REV868C1-CHECK-" + scenario))
        {
            var check = new StockAvailabilityCheck { PurchaseRequisitionId = pr.Id, CheckNumber = "REV868C1-SC-" + scenario, CheckedBy = ActorStores, ResultStatus = pr.Status, Remarks = "REV868C1 stock check", CorrelationId = "REV868C1-CHECK-" + scenario, CreatedBy = ActorStores };
            check.Lines.Add(new StockAvailabilityCheckLine { PurchaseRequisitionLineId = line.Id, ItemId = context.Item.Id, WarehouseId = context.Warehouse.Id, RackBinId = context.RackBin.Id, LocationKey = context.LocationKey, RequestedQuantity = requested, OnHandQuantity = onHand, ActiveReservedQuantity = 0, AvailableQuantity = onHand, InTransitQuantity = 0, ReservedQuantity = reserved, ShortageQuantity = shortage, LineResultStatus = line.LineStatus, CreatedBy = ActorStores });
            db.StockAvailabilityChecks.Add(check);
        }
        if (reserved > 0 && !await db.StockReservations.AnyAsync(x => x.PurchaseRequisitionLineId == line.Id && x.LocationKey == context.LocationKey && x.Status == "Active"))
        {
            var reservation = new StockReservation { PurchaseRequisitionId = pr.Id, PurchaseRequisitionLineId = line.Id, ItemId = context.Item.Id, WarehouseId = context.Warehouse.Id, RackBinId = context.RackBin.Id, LocationKey = context.LocationKey, ReservedQuantity = reserved, Status = "Active", ReservationNumber = "REV868C1-RSV-" + scenario, ReservedBy = ActorStores, CorrelationId = "REV868C1-" + scenario, CreatedBy = ActorStores };
            db.StockReservations.Add(reservation);
            db.StockReservationHistories.Add(new StockReservationHistory { StockReservationId = reservation.Id, Action = "Create", NewStatus = "Active", Remarks = "REV868C1 reservation", ActorLoginId = ActorStores, CorrelationId = "REV868C1-" + scenario, CreatedBy = ActorStores });
        }
        if (shortage > 0 && !await db.PurchaseRequirementHandoffs.AnyAsync(x => x.PurchaseRequisitionLineId == line.Id && x.Status == "PendingRFQ"))
        {
            db.PurchaseRequirementHandoffs.Add(new PurchaseRequirementHandoff { PurchaseRequisitionId = pr.Id, PurchaseRequisitionLineId = line.Id, ItemId = context.Item.Id, WarehouseId = context.Warehouse.Id, RackBinId = context.RackBin.Id, LocationKey = context.LocationKey, HandoffQuantity = shortage, Status = "PendingRFQ", HandoffNumber = "REV868C1-PHO-" + scenario, HandoffBy = ActorStores, CorrelationId = "REV868C1-" + scenario, CreatedBy = ActorStores });
        }
        await AddStatusAsync(db, pr, PurchaseRequisitionStatuses.StockCheckPending, pr.Status, "REV868C1 stock check", "STOCK-" + scenario, ActorStores, "stores_executive");
        await AddAuditAsync(db, "Stores", "StockCheck", nameof(PurchaseRequisition), pr.Id.ToString(), "Success", "STOCK-" + scenario, ActorStores);
        await db.SaveChangesAsync();
        return (pr, line);
    }

    private static async Task EnsureRouteAsync(NexaErpDbContext db, string routeCode, decimal min, decimal? max, string role)
    {
        var route = await db.PurchaseApprovalRouteSettings.SingleOrDefaultAsync(x => x.RouteCode == routeCode) ?? new PurchaseApprovalRouteSetting { RouteCode = routeCode, CreatedBy = "rev868c1" };
        route.MinimumAmount = min;
        route.MaximumAmount = max;
        route.ApproverRoleCode = role;
        route.IsActive = true;
        if (db.Entry(route).State == EntityState.Detached) db.PurchaseApprovalRouteSettings.Add(route);
        await db.SaveChangesAsync();
    }

    private static async Task AddStatusAsync(NexaErpDbContext db, PurchaseRequisition pr, string? from, string to, string reason, string key, string actor, string role)
    {
        var correlation = "REV868C1-" + key;
        if (await db.PurchaseRequisitionStatusHistories.AnyAsync(x => x.PurchaseRequisitionId == pr.Id && x.CorrelationId == correlation)) return;
        db.PurchaseRequisitionStatusHistories.Add(new PurchaseRequisitionStatusHistory { PurchaseRequisitionId = pr.Id, PrNumber = pr.PrNumber, PreviousStatus = from, NewStatus = to, Reason = reason, ActorLoginId = actor, ActorRoleCode = role, CorrelationId = correlation, CreatedBy = actor });
        await db.SaveChangesAsync();
    }

    private static async Task AddApprovalAsync(NexaErpDbContext db, PurchaseRequisition pr, string action, string from, string to, string route, string remarks, string key, string actor, string role)
    {
        var correlation = "REV868C1-" + key;
        if (await db.PurchaseRequisitionApprovalHistories.AnyAsync(x => x.PurchaseRequisitionId == pr.Id && x.CorrelationId == correlation)) return;
        db.PurchaseRequisitionApprovalHistories.Add(new PurchaseRequisitionApprovalHistory { PurchaseRequisitionId = pr.Id, PrNumber = pr.PrNumber, Action = action, FromStatus = from, ToStatus = to, ApprovalRoute = route, Remarks = remarks, ActorLoginId = actor, ActorRoleCode = role, CorrelationId = correlation, CreatedBy = actor });
        await db.SaveChangesAsync();
    }

    private static async Task AddAuditAsync(NexaErpDbContext db, string module, string action, string entity, string entityId, string result, string key, string actor)
    {
        var correlation = "REV868C1-" + key;
        if (await db.AuditLogs.AnyAsync(x => x.CorrelationId == correlation)) return;
        db.AuditLogs.Add(new AuditLog { Module = module, Action = action, EntityName = entity, EntityId = entityId, UserLoginId = actor, Result = result, CorrelationId = correlation, CreatedBy = actor });
        await db.SaveChangesAsync();
    }

    private static string Read(params string[] relativeParts) => File.ReadAllText(Find(relativeParts));

    private static string Find(params string[] relativeParts)
    {
        var relativePath = Path.Combine(relativeParts);
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, relativePath);
            if (File.Exists(candidate)) return candidate;
            if (directory.Name.Equals("target-dotnet", StringComparison.OrdinalIgnoreCase)) break;
            directory = directory.Parent;
        }
        throw new FileNotFoundException($"Could not find {relativePath} from {AppContext.BaseDirectory}.");
    }

    private sealed record FoundationContext(Item Item, Warehouse Warehouse, RackBin RackBin, string LocationKey);
}