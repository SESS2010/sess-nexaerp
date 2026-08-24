// Inert preserved source: excluded from SESS.NexaERP.Tests by location.
// Original active-test source: tests/SESS.NexaERP.Tests/Rev869BPurchaseCorrectionTests.cs
// These assertions describe the archived pre-advance EF migration history beside this folder.
#if false

    [Fact]
    public void DatabaseStatusSetsMatchCanonicalAggregateSets()
    {
        Assert.Equal(Rev869BStatusContracts.Quotation.Order(), CanonicalConstraintValues("CK_vendor_quotation_status").Order());
        Assert.Equal(Rev869BStatusContracts.Comparison.Order(), CanonicalConstraintValues("CK_comparison_status").Order());
        Assert.Equal(Rev869BStatusContracts.PurchaseOrder.Order(), CanonicalConstraintValues("CK_purchase_order_status").Order());
        Assert.Equal(Rev869BStatusContracts.MaterialFollowUp.Order(), CanonicalConstraintValues("CK_material_followup_quantity").Where(x => x != "OrderedQuantitySnapshot").Order());
        Assert.DoesNotContain("Recommended", CanonicalConstraintValues("CK_comparison_status"));
        Assert.DoesNotContain("PendingReapproval", Migration + Service);
        Assert.DoesNotContain("PendingTechnicalVerification", Migration + Service);
    }

    [Fact]
    public void MigrationOwnsImmutableAndCrossParentFailClosedGuards()
    {
        Assert.Equal(79, Count(MigrationInstall, "CREATE TRIGGER trg_rev869b_") + Count(MigrationInstall, "CREATE CONSTRAINT TRIGGER trg_rev869b_"));
        Assert.Equal(2, Count(MigrationInstall, "CREATE TRIGGER trg_rev869b_down_"));
        Assert.Contains("rev869b_guard_controlled_snapshot", Migration);
        Assert.Contains("rev869b_enforce_transition", Migration);
        Assert.Contains("Purchase order pre-issue snapshot is incomplete or does not reconcile", Migration);
        Assert.Contains("rev869b_validate_parent_contract", Migration);
        foreach (var message in new[] { "Quotation line parent contract mismatch", "Comparison line parent contract mismatch", "Purchase order parent contract mismatch", "Purchase order line parent contract mismatch", "Material follow-up parent contract mismatch" }) Assert.Contains(message, Migration);
        Assert.Contains("DROP FUNCTION IF EXISTS nexa.rev869b_validate_parent_contract", Migration);
        Assert.Contains("DROP FUNCTION IF EXISTS nexa.rev869b_guard_controlled_snapshot", Migration);
    }

    [Fact]
    public void MigrationRetainsExactSourceOwnedSeedCountsAndNoBusinessSeeds()
    {
        var normalizedCorrect = Migration.Replace(string.Concat((char)13, (char)10), string.Concat((char)10));
        var permissionInsertCorrect = string.Join((char)10, "migrationBuilder.InsertData(", "                schema: \"nexa\",", "                table: \"role_page_permissions\"");
        var permissionStart = normalizedCorrect.IndexOf(permissionInsertCorrect, StringComparison.Ordinal);
        Assert.True(permissionStart >= 0);
        var permissionBlock = normalizedCorrect[permissionStart..normalizedCorrect.IndexOf("migrationBuilder.Sql(", permissionStart, StringComparison.Ordinal)];
        Assert.Equal(29, Regex.Matches(permissionBlock, @"(?m)^\s*\{ new Guid\(").Count);
        Assert.Contains("DEPARTMENT_MANAGER", Migration);
        foreach (var prohibited in new[] { "INSERT INTO nexa.vendors", "INSERT INTO nexa.employees", "INSERT INTO nexa.vendor_quotations", "INSERT INTO nexa.purchase_orders" }) Assert.DoesNotContain(prohibited, Migration, StringComparison.OrdinalIgnoreCase);
    }
#endif
