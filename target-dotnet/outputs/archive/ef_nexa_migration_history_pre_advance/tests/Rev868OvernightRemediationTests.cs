// Inert preserved source: excluded from SESS.NexaERP.Tests by location.
// Original active-test source: tests/SESS.NexaERP.Tests/Rev868OvernightRemediationTests.cs
// These assertions describe the archived pre-advance EF migration history beside this folder.
#if false

    [Fact]
    public void Rev868_stock_check_source_blocks_over_reservation_and_duplicate_location_allocations()
    {
        var support = Read("src", "SESS.NexaERP.Api", "Endpoints", "PurchaseRequisitionSupport.cs");
        var context = Read("src", "SESS.NexaERP.Infrastructure", "Persistence", "NexaErpDbContext.cs");
        var migration = Read("src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations", "20260808190920_Rev868PurchaseLocationAllocationCorrection.cs");

        Assert.Contains("reservation exceeds requested quantity", support);
        Assert.Contains("duplicate warehouse/bin allocation is not allowed", support);
        Assert.Contains("active reservation already exists for warehouse/bin allocation", support);
        Assert.Contains("PurchaseRequisitionLineId, x.LocationKey, x.Status", context);
        Assert.Contains("IX_stock_reservations_PurchaseRequisitionLineId_LocationKey_St", migration);
        Assert.Contains("CK_pr_lines_reconcile_requested", context);
        Assert.Contains("CK_stock_check_lines_quantities_valid", context);
    }

    [Fact]
    public void Rev868_numbering_source_uses_financial_year_sequence_and_unique_constraints()
    {
        var helper = Read("src", "SESS.NexaERP.Api", "Endpoints", "PurchaseRequisitionEndpointHelpers.cs");
        var context = Read("src", "SESS.NexaERP.Infrastructure", "Persistence", "NexaErpDbContext.cs");
        var migration = Read("src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations", "20260808190920_Rev868PurchaseLocationAllocationCorrection.cs");

        Assert.Contains("FinancialYear", helper);
        Assert.Contains("PurchaseNumberSequences", helper);
        Assert.Contains("LastNumber", helper);
        Assert.Contains("PrSequence", context);
        Assert.Contains("OrganizationId, x.FinancialYear, x.Prefix", context);
        Assert.Contains("purchase_number_sequences", migration);
        Assert.Contains("IX_purchase_requisitions_OrganizationId_FinancialYear_PrSequen", migration);
    }
#endif
