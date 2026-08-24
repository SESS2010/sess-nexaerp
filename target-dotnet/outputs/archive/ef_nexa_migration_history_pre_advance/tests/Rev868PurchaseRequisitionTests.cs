// Inert preserved source: excluded from SESS.NexaERP.Tests by location.
// Original active-test source: tests/SESS.NexaERP.Tests/Rev868PurchaseRequisitionTests.cs
// These assertions describe the archived pre-advance EF migration history beside this folder.
#if false

    [Fact]
    public void Rev868_corrected_migration_source_contains_location_level_schema_changes()
    {
        var foundation = File.ReadAllText(FindTargetDotnetFile(Path.Combine("src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations", "20260808182945_Rev868PurchaseRequisitionFoundation.cs")));
        var migration = File.ReadAllText(FindTargetDotnetFile(Path.Combine("src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations", "20260808190920_Rev868PurchaseLocationAllocationCorrection.cs")));
        var designer = File.ReadAllText(FindTargetDotnetFile(Path.Combine("src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations", "20260808190920_Rev868PurchaseLocationAllocationCorrection.Designer.cs")));
        var snapshot = File.ReadAllText(FindTargetDotnetFile(Path.Combine("src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations", "NexaErpDbContextModelSnapshot.cs")));
        var sql = File.ReadAllText(FindTargetDotnetFile(Path.Combine("outputs", "rev868_purchase_requisition_foundation_idempotent.sql")));

        Assert.Contains("purchase_number_sequences", migration);
        Assert.Contains("IX_stock_reservations_PurchaseRequisitionLineId_LocationKey_St", migration);
        Assert.Contains("IX_stock_availability_check_lines_StockAvailabilityCheckId_Pur", migration);
        Assert.Contains("CK_stock_check_lines_quantities_valid", migration);
        Assert.Contains("CK_pr_lines_reconcile_requested", migration);
        Assert.Contains("CK_purchase_route_limits_valid", migration);
        Assert.DoesNotContain("purchase_number_sequences", foundation);
        Assert.Contains("purchase_number_sequences", designer);
        Assert.Contains("purchase_number_sequences", snapshot);
        Assert.Contains("IX_stock_reservations_PurchaseRequisitionLineId_LocationKey_St", sql);
        Assert.Contains("20260808190920_Rev868PurchaseLocationAllocationCorrection", sql);
    }
#endif
