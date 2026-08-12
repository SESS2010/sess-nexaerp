using System.Reflection;
using Microsoft.EntityFrameworkCore.Migrations;
using SESS.NexaERP.Domain.Purchase;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed class Rev869BPurchaseFoundationTests
{
    private static readonly string Root = FindRoot();
    private static readonly string DomainSource = Read("src", "SESS.NexaERP.Domain", "Purchase", "Rev869BPurchaseTransactions.cs");
    private static readonly string ServiceSource = Read("src", "SESS.NexaERP.Infrastructure", "Purchase", "EfRev869BPurchaseService.cs") + Read("src", "SESS.NexaERP.Infrastructure", "Purchase", "EfRev869BPurchaseService.RfqQuotation.cs") + Read("src", "SESS.NexaERP.Infrastructure", "Purchase", "EfRev869BPurchaseService.ComparisonPo.cs");
    private static readonly string MappingSource = Read("src", "SESS.NexaERP.Infrastructure", "Persistence", "NexaErpDbContext.Rev869B.cs");
    private static readonly string ApiSource = Read("src", "SESS.NexaERP.Api", "Endpoints", "Rev869BPurchaseEndpoints.cs");
    private static readonly string MigrationPath = Directory.GetFiles(Path.Combine(Root, "src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations"), "*Rev869BRfqQuotationComparisonPurchaseOrderFoundation.cs").Single(x => !x.EndsWith(".Designer.cs", StringComparison.Ordinal));
    private static readonly string MigrationSource = File.ReadAllText(MigrationPath);
    private static readonly string MigrationInstallSource = MigrationSource +
        Read("src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations", "Rev869BDatabaseSafetySql.cs") +
        Read("src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations", "Rev869BDatabaseLifecycleSql.cs");

    [Theory]
    [InlineData(0, "MANAGER")]
    [InlineData(50000, "MANAGER")]
    [InlineData(50000.000001, "TECHNICAL_DIRECTOR")]
    [InlineData(500000, "TECHNICAL_DIRECTOR")]
    [InlineData(500000.000001, "MANAGING_DIRECTOR")]
    public void ApprovalBoundariesAreExact(decimal value, string route)
    {
        Assert.Equal(route, Rev869BApprovalRoutes.Resolve(value, Rev869BSeedData.ApprovalPolicies, new DateOnly(2026, 8, 11), "SESS"));
    }

    [Fact]
    public void MissingOrAmbiguousApprovalPolicyFailsClosed()
    {
        Assert.Throws<InvalidOperationException>(() => Rev869BApprovalRoutes.Resolve(1m, Array.Empty<PurchaseTransactionApprovalPolicy>(), new DateOnly(2026, 8, 11), "SESS"));
        var duplicate = Rev869BSeedData.ApprovalPolicies.Concat(Rev869BSeedData.ApprovalPolicies.Take(1));
        Assert.Throws<InvalidOperationException>(() => Rev869BApprovalRoutes.Resolve(1m, duplicate, new DateOnly(2026, 8, 11), "SESS"));
    }

    [Fact]
    public void CommercialCalculationPreservesComponentsAndApprovedRounding()
    {
        var result = Rev869BCommercialCalculator.Calculate(new(3m, 100m, 10m, 2m, 3m, 4m, 5m, 9m, 9m, 0m, 1m, 0.005m, 2));
        Assert.Equal(304m, result.TaxableValue); Assert.Equal(27.36m, result.CgstValue); Assert.Equal(27.36m, result.SgstValue); Assert.Equal(3.04m, result.CessValue); Assert.Equal(361.77m, result.TotalPayableValue);
        Assert.Throws<InvalidOperationException>(() => Rev869BCommercialCalculator.Calculate(new(1m, 1m, 2m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 2)));
        Assert.Throws<InvalidOperationException>(() => Rev869BCommercialCalculator.Calculate(new(1m, 1m, 0m, 0m, 0m, 0m, 0m, 101m, 0m, 0m, 0m, 0m, 2)));
    }

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
        Assert.Equal(37, Count(MigrationInstallSource, "CREATE TRIGGER trg_rev869b_"));
        Assert.Equal(2, Count(MigrationInstallSource, "CREATE TRIGGER trg_rev869b_down_"));
        Assert.Contains("DROP FUNCTION IF EXISTS nexa.rev869b_reject_immutable_mutation", MigrationSource);
    }

    [Fact]
    public void ServiceReusesPendingRfqAndPreventsDuplicateAndOverOrder()
    {
        Assert.Contains("PurchaseRequirementHandoffs", ServiceSource); Assert.Contains("PendingRFQ", ServiceSource);
        Assert.Contains("IsolationLevel.Serializable", ServiceSource); Assert.Contains("IdempotencyKey", ServiceSource); Assert.Contains("OrderedQuantityAsync", ServiceSource);
        Assert.Contains("Cumulative PO quantity exceeds approved outstanding quantity", ServiceSource); Assert.Contains("Duplicate PendingRFQ handoff", ServiceSource);
    }

    [Fact]
    public void VendorTaxLateQuoteAndCurrencyRulesFailClosed()
    {
        Assert.Contains("vendors.IsEligibleAsync", ServiceSource); Assert.Contains("taxes.ResolveAsync", ServiceSource); Assert.Contains("TaxJurisdictions.IndiaGst", ServiceSource);
        Assert.Contains("Currency conversion is not configured", ServiceSource); Assert.Contains("Late quotation/revision requires Purchase Manager authorization", ServiceSource);
        Assert.Contains("Vendor qualification is missing or expired", ServiceSource);
    }

    [Fact]
    public void TechnicalCommercialAndVersionedPoDutiesStaySeparated()
    {
        Assert.Contains("QuotationTechnicalVerification", DomainSource); Assert.Contains("CommercialComparison", DomainSource); Assert.Contains("RecommendedVendorQuotationId", DomainSource);
        Assert.Contains("TechnicallyCompliant", ServiceSource); Assert.DoesNotContain("OrderBy(x => x.TotalPayableValue).First", ServiceSource);
        Assert.Contains("PendingApproval", ServiceSource); Assert.Contains("PreviousVersionId", ServiceSource); Assert.Contains("Superseded", ServiceSource); Assert.Contains("MaterialFollowUpHandoffs", ServiceSource);
    }

    [Fact]
    public void ApprovalAndScopeRulesRejectMissingIdentitySelfApprovalAndUnauthorizedRecords()
    {
        Assert.Contains("A unique active employee identity mapping is required", ServiceSource); Assert.Contains("Self-approval is prohibited", ServiceSource);
        Assert.Contains("DepartmentApprovalMappings", ServiceSource); Assert.Contains("mappings.Count != 1", ServiceSource); Assert.Contains("scopes.AuthorizeAsync", ServiceSource); Assert.Contains("RecordScope", ServiceSource);
        Assert.Contains("RequireResolvedEmployeeAndScope", ApiSource); Assert.Contains("RequirePagePermission", ApiSource); Assert.Contains("RequireAuthorization", ApiSource);
    }

    [Fact]
    public void RejectionRevisionCancellationAndHistoriesRequireRemarks()
    {
        Assert.Contains("RequiredRemarks", ServiceSource); Assert.Contains("PurchaseTransactionApprovalHistories", ServiceSource); Assert.Contains("PurchaseOrderHistories", ServiceSource); Assert.Contains("PurchaseTransactionStatusHistories", ServiceSource);
        Assert.Contains("CancellationReason", ServiceSource); Assert.Contains("RequestRevision", ApiSource); Assert.Contains("Resubmit", ApiSource);
    }

    [Fact]
    public void MigrationIsDiscoverableThroughEfMetadata()
    {
        var type = typeof(NexaErpDbContext).Assembly.GetTypes().Single(x => x.Name.EndsWith("Rev869BRfqQuotationComparisonPurchaseOrderFoundation", StringComparison.Ordinal) && !x.Name.Contains("Attribute", StringComparison.Ordinal));
        var migration = type.GetCustomAttribute<MigrationAttribute>(); Assert.NotNull(migration); Assert.EndsWith("_Rev869BRfqQuotationComparisonPurchaseOrderFoundation", migration!.Id, StringComparison.Ordinal);
    }

    [Fact]
    public void ExplicitExclusionsRemainAbsent()
    {
        foreach (var prohibited in new[] { "GoodsReceipt", "GRN", "InventoryLedger", "MaterialIssue", "MaterialReturn", "CustomerMaster", "ProjectMaster", "Rev869C" }) Assert.DoesNotContain(prohibited, DomainSource + ServiceSource, StringComparison.OrdinalIgnoreCase);
    }

    private static int Count(string source, string value) => source.Split(value, StringSplitOptions.None).Length - 1;
    private static string Read(params string[] parts) => File.ReadAllText(Path.Combine(new[] { Root }.Concat(parts).ToArray()));
    private static string FindRoot() { var directory = new DirectoryInfo(AppContext.BaseDirectory); while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "SESS.NexaERP.slnx"))) directory = directory.Parent; return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found."); }
}
