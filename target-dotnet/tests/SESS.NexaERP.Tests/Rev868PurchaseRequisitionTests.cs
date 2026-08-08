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


