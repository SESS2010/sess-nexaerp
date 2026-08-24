// Inert preserved source: excluded from SESS.NexaERP.Tests by location.
// Original active-test source: tests/SESS.NexaERP.Tests/Rev869BPurchaseFoundationTests.cs
// These assertions describe the archived pre-advance EF migration history beside this folder.
#if false

    [Fact]
    public void SeedMatrixIsDeterministicAndLeastPrivilege()
    {
        Assert.Equal(4, Rev869BSeedData.Pages.Length); Assert.Equal(3, Rev869BSeedData.ApprovalPolicies.Length); Assert.Equal(29, Rev869BSeedData.RolePagePermissions.Count);
        Assert.All(Rev869BSeedData.RolePagePermissions, x => Assert.Equal("migration-rev869b", x.CreatedBy));
        Assert.DoesNotContain(Rev869BSeedData.RolePagePermissions, x => !x.CanView && !x.CanCreate && !x.CanUpdate && !x.CanSubmit && !x.CanIssue && !x.CanVerify && !x.CanApprove && !x.CanReject && !x.CanRequestClarification && !x.CanRequestRevision && !x.CanResubmit && !x.CanCancel && !x.CanDeactivate && !x.CanPrint && !x.CanDownload && !x.CanExport && !x.CanUploadAttachment && !x.CanReplaceAttachment && !x.CanViewCommercialValues && !x.CanViewAuditHistory && !x.HasFullControl);
        var stores = Rev869BSeedData.RolePagePermissions.Where(x => x.CanView && !x.CanCreate && !x.CanUpdate).ToList(); Assert.NotEmpty(stores);
        Assert.Contains("DEPARTMENT_MANAGER", MigrationSource); Assert.Contains("CanRequestClarification", MigrationSource);
    }

    [Fact]
    public void MigrationContainsExactlyFifteenOwnedTablesAndNoLegacyTableMutation()
    {
        var tables = new[] { "request_for_quotations", "request_for_quotation_lines", "rfq_vendor_invitations", "vendor_quotations", "vendor_quotation_lines", "quotation_technical_verifications", "commercial_comparisons", "commercial_comparison_lines", "purchase_transaction_approval_history", "purchase_orders", "purchase_order_lines", "purchase_order_history", "material_followup_handoffs", "purchase_transaction_status_history", "purchase_transaction_approval_policies" };
        var normalized = MigrationSource.Replace("\r\n", "\n");
        Assert.Equal(15, tables.Length); foreach (var table in tables) { Assert.Contains($"name: \"{table}\"", normalized); Assert.Contains($"DropTable(\n                name: \"{table}\"", normalized); }
        Assert.DoesNotContain("DropTable(\n                name: \"purchase_requisitions\"", normalized);
        Assert.DoesNotContain("AlterColumn", MigrationSource);
        Assert.Equal(1, Count(MigrationSource, "AddColumn<bool>")); Assert.Equal(1, Count(MigrationSource, "DropColumn(name: \"CanIssue\""));
    }

    [Fact]
    public void MigrationAndMappingEnforceUniquenessConcurrencyAndImmutability()
    {
        Assert.Contains("IX_purchase_orders_OrganizationId_PoNumber_RevisionNumber", MigrationSource);
        Assert.Contains("\\\"IsCurrentVersion\\\" = TRUE", MigrationSource); Assert.Contains("\\\"IsCurrentRevision\\\" = TRUE", MigrationSource);
        Assert.Contains("IsConcurrencyToken", MappingSource); Assert.Contains("rev869b_reject_immutable_mutation", MigrationSource);
        Assert.Equal(40, Count(MigrationInstallSource, "CREATE TRIGGER trg_rev869b_"));
        Assert.Equal(2, Count(MigrationInstallSource, "CREATE TRIGGER trg_rev869b_down_"));
        Assert.Contains("DROP FUNCTION IF EXISTS nexa.rev869b_reject_immutable_mutation", MigrationSource);
    }

    [Fact]
    public void MigrationIsDiscoverableThroughEfMetadata()
    {
        var type = typeof(NexaErpDbContext).Assembly.GetTypes().Single(x => x.Name.EndsWith("Rev869BRfqQuotationComparisonPurchaseOrderFoundation", StringComparison.Ordinal) && !x.Name.Contains("Attribute", StringComparison.Ordinal));
        var migration = type.GetCustomAttribute<MigrationAttribute>(); Assert.NotNull(migration); Assert.EndsWith("_Rev869BRfqQuotationComparisonPurchaseOrderFoundation", migration!.Id, StringComparison.Ordinal);
    }
#endif
