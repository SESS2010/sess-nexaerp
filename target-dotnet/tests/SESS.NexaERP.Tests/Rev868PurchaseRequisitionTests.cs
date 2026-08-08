using SESS.NexaERP.Api.Endpoints;
using SESS.NexaERP.Domain.Purchase;

namespace SESS.NexaERP.Tests;

public sealed class Rev868PurchaseRequisitionTests
{
    [Theory]
    [InlineData(0, PurchaseRequisitionApprovalRoutes.Manager)]
    [InlineData(50000, PurchaseRequisitionApprovalRoutes.Manager)]
    [InlineData(50001, PurchaseRequisitionApprovalRoutes.TechnicalDirector)]
    [InlineData(500000, PurchaseRequisitionApprovalRoutes.TechnicalDirector)]
    [InlineData(500001, PurchaseRequisitionApprovalRoutes.ManagingDirector)]
    public void Rev868_amount_based_pr_approval_route_is_configured(decimal total, string expected)
    {
        Assert.Equal(expected, PurchaseRequisitionEndpoints.RouteFor(total));
    }

    [Theory]
    [InlineData(100, 25, 75)]
    [InlineData(20, 25, 0)]
    [InlineData(0, 0, 0)]
    public void Rev868_available_quantity_uses_ledger_minus_active_reservation(decimal onHand, decimal activeReserved, decimal expected)
    {
        Assert.Equal(expected, PurchaseRequisitionEndpoints.AvailableQuantity(onHand, activeReserved));
    }

    [Theory]
    [InlineData(10, 25, 10, 0)]
    [InlineData(25, 10, 10, 15)]
    [InlineData(25, 0, 0, 25)]
    public void Rev868_reservation_and_handoff_quantities_reconcile(decimal requested, decimal available, decimal expectedReserved, decimal expectedShortage)
    {
        var reserved = PurchaseRequisitionEndpoints.ReserveQuantity(requested, available);

        Assert.Equal(expectedReserved, reserved);
        Assert.Equal(expectedShortage, PurchaseRequisitionEndpoints.ShortageQuantity(requested, reserved));
    }

    [Fact]
    public void Rev868_page_master_seed_contains_only_pr_stock_check_reservation_and_handoff_pages()
    {
        var source = File.ReadAllText(FindTargetDotnetFile(Path.Combine("src", "SESS.NexaERP.Infrastructure", "Persistence", "FoundationSeedData.cs")));

        Assert.Contains("purchase.requisitions", source);
        Assert.Contains("purchase.requisition-approvals", source);
        Assert.Contains("stores.stock-check", source);
        Assert.Contains("stores.reservations", source);
        Assert.Contains("purchase.requirement-handoff", source);
    }

    [Fact]
    public void Rev868_endpoint_scope_does_not_implement_rfq_po_grn_or_stock_issue_transactions()
    {
        var endpoint = File.ReadAllText(FindTargetDotnetFile(Path.Combine("src", "SESS.NexaERP.Api", "Endpoints", "PurchaseRequisitionEndpoints.cs")));
        var helper = File.ReadAllText(FindTargetDotnetFile(Path.Combine("src", "SESS.NexaERP.Api", "Endpoints", "PurchaseRequisitionEndpointHelpers.cs")));
        var support = File.ReadAllText(FindTargetDotnetFile(Path.Combine("src", "SESS.NexaERP.Api", "Endpoints", "PurchaseRequisitionSupport.cs")));
        var combined = endpoint + helper + support;

        Assert.DoesNotContain("/rfq", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/purchase-orders", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/grn", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("StockIssue", combined, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("PendingRFQ", combined);
    }

    [Fact]
    public void Rev868_domain_contains_no_direct_stock_balance_entity_or_hard_delete_contract()
    {
        var source = File.ReadAllText(FindTargetDotnetFile(Path.Combine("src", "SESS.NexaERP.Domain", "Purchase", "PurchaseRequisition.cs")));

        Assert.DoesNotContain("Delete", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("StockBalance", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("PurchaseRequirementHandoff", source);
        Assert.Contains("StockReservation", source);
    }

    [Fact]
    public void Rev868_secure_helper_has_expected_target_guard_and_sql_quoting()
    {
        var source = File.ReadAllText(FindTargetDotnetFile(Path.Combine("tools", "apply-rev868-secure.ps1")));

        Assert.Contains("20260808182945_Rev868PurchaseRequisitionFoundation", source);
        Assert.Contains("REV868 helper expected database guard failed", source);
        Assert.Contains("sess_nexaerp", source);
        Assert.Contains("PreflightOnly", source);
        Assert.Contains("GenerateSqlOnly", source);
        Assert.Contains("select \"MigrationId\"", source);
        Assert.Contains("from \"public\".\"__EFMigrationsHistory\"", source);
        Assert.Contains("order by \"MigrationId\"", source);
        Assert.DoesNotContain("\"\"MigrationId\"\"", source);
    }

    [Fact]
    public void Rev868_secure_helper_contains_no_database_create_drop_restore_or_dml_operations()
    {
        var source = File.ReadAllText(FindTargetDotnetFile(Path.Combine("tools", "apply-rev868-secure.ps1")));
        var forbidden = new[]
        {
            "create database",
            "drop database",
            "pg_restore",
            "truncate ",
            "delete from",
            "insert into",
            "update nexa",
            "restore database"
        };

        foreach (var token in forbidden)
        {
            Assert.DoesNotContain(token, source, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("ef database update $migrationName", source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Rev868_location_key_distinguishes_warehouse_only_and_bin_controlled_allocations()
    {
        var warehouseId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var rackBinId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        Assert.Equal("W:11111111111111111111111111111111:B:NONE", PurchaseRequisitionEndpoints.LocationKey(warehouseId, null));
        Assert.Equal("W:11111111111111111111111111111111:B:22222222222222222222222222222222", PurchaseRequisitionEndpoints.LocationKey(warehouseId, rackBinId));
    }

    [Theory]
    [InlineData(25, 10, 15)]
    [InlineData(25, 25, 0)]
    [InlineData(25, 40, 0)]
    public void Rev868_shortage_uses_requested_minus_total_active_reserved(decimal requested, decimal activeReserved, decimal expectedShortage)
    {
        Assert.Equal(expectedShortage, PurchaseRequisitionEndpoints.ReconciledShortage(requested, activeReserved));
    }

    [Theory]
    [InlineData(0, 10, true)]
    [InlineData(1, 1, true)]
    [InlineData(10, null, true)]
    [InlineData(-1, 10, false)]
    [InlineData(10, 9, false)]
    public void Rev868_approval_route_limits_are_validated(decimal min, int? max, bool expected)
    {
        decimal? maxLimit = max.HasValue ? max.Value : null;
        Assert.Equal(expected, PurchaseRequisitionEndpoints.IsRouteLimitValid(min, maxLimit));
    }

    [Fact]
    public void Rev868_approval_route_ranges_cannot_overlap()
    {
        (decimal Min, decimal? Max)[] active = [(0m, 50000m), (50001m, 500000m)];

        Assert.True(PurchaseRequisitionEndpoints.HasOverlappingRoute(40000, 60000, active));
        Assert.False(PurchaseRequisitionEndpoints.HasOverlappingRoute(500001, null, active));
    }

    [Fact]
    public void Rev868_source_contains_location_level_stock_check_and_reservation_guards()
    {
        var support = File.ReadAllText(FindTargetDotnetFile(Path.Combine("src", "SESS.NexaERP.Api", "Endpoints", "PurchaseRequisitionSupport.cs")));
        var context = File.ReadAllText(FindTargetDotnetFile(Path.Combine("src", "SESS.NexaERP.Infrastructure", "Persistence", "NexaErpDbContext.cs")));
        var domain = File.ReadAllText(FindTargetDotnetFile(Path.Combine("src", "SESS.NexaERP.Domain", "Purchase", "PurchaseRequisition.cs")));

        Assert.Contains("IsolationLevel.Serializable", support);
        Assert.Contains("ResolveLocations", support);
        Assert.Contains("RackBinId", domain);
        Assert.Contains("LocationKey", domain);
        Assert.Contains("PurchaseNumberSequence", domain);
        Assert.Contains("PurchaseNumberSequences", context);
        Assert.Contains("PurchaseRequisitionLineId, x.LocationKey, x.Status", context);
        Assert.DoesNotContain("entity.HasIndex(x => new { x.PurchaseRequisitionLineId, x.Status }).IsUnique().HasFilter(\"\\\"Status\\\" = 'Active'\")", context);
        Assert.Contains("CK_pr_lines_reconcile_requested", context);
    }

    [Fact]
    public void Rev868_helper_sql_includes_prerequisite_and_absence_checks_without_write_sql()
    {
        var source = File.ReadAllText(FindTargetDotnetFile(Path.Combine("tools", "apply-rev868-secure.ps1")));

        Assert.Contains("Required migration prerequisites through REV867C1", source);
        Assert.Contains("20260808160435_Rev867C1Corrections", source);
        Assert.Contains("20260808182945_Rev868PurchaseRequisitionFoundation", source);
        Assert.Contains("select \"MigrationId\"", source);
        Assert.DoesNotContain("\"\"MigrationId\"\"", source);
        Assert.DoesNotContain("drop database", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("create database", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("pg_restore", source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Rev868_corrected_migration_source_contains_location_level_schema_changes()
    {
        var migration = File.ReadAllText(FindTargetDotnetFile(Path.Combine("src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations", "20260808182945_Rev868PurchaseRequisitionFoundation.cs")));
        var sql = File.ReadAllText(FindTargetDotnetFile(Path.Combine("outputs", "rev868_purchase_requisition_foundation_idempotent.sql")));

        Assert.Contains("purchase_number_sequences", migration);
        Assert.Contains("IX_stock_reservations_Line_Location_Status", migration);
        Assert.Contains("IX_stock_availability_check_lines_Check_Line_Location", migration);
        Assert.Contains("CK_stock_check_lines_quantities_valid", migration);
        Assert.Contains("CK_pr_lines_reconcile_requested", migration);
        Assert.Contains("CK_purchase_route_limits_valid", migration);
        Assert.Contains("DROP INDEX IF EXISTS nexa.\"IX_stock_reservations_PurchaseRequisitionLineId_Status\"", sql);
        Assert.Contains("IX_stock_reservations_Line_Location_Status", sql);
    }

    private static string FindTargetDotnetFile(string relativePath)
    {
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
}

