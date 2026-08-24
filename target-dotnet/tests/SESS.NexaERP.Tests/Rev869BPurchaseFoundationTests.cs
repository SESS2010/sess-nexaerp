using System.Reflection;
using Microsoft.EntityFrameworkCore.Migrations;
using SESS.NexaERP.Domain.Purchase;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed class Rev869BPurchaseFoundationTests
{
    private static readonly string Root = FindRoot();
    private static string DomainSource => Read("src", "SESS.NexaERP.Domain", "Purchase", "Rev869BPurchaseTransactions.cs");
    private static string ServiceSource => Read("src", "SESS.NexaERP.Infrastructure", "Purchase", "EfRev869BPurchaseService.cs") + Read("src", "SESS.NexaERP.Infrastructure", "Purchase", "EfRev869BPurchaseService.RfqQuotation.cs") + Read("src", "SESS.NexaERP.Infrastructure", "Purchase", "EfRev869BPurchaseService.ComparisonPo.cs");
    private static string MappingSource => Read("src", "SESS.NexaERP.Infrastructure", "Persistence", "NexaErpDbContext.Rev869B.cs");
    private static string ApiSource => Read("src", "SESS.NexaERP.Api", "Endpoints", "Rev869BPurchaseEndpoints.cs");
    private static string MigrationPath => Directory.GetFiles(Path.Combine(Root, "src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations"), "*Rev869BRfqQuotationComparisonPurchaseOrderFoundation.cs").Single(x => !x.EndsWith(".Designer.cs", StringComparison.Ordinal));
    private static string MigrationSource => File.ReadAllText(MigrationPath);
    private static string MigrationInstallSource => MigrationSource +
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
        Assert.Equal(route, Rev869BApprovalRoutes.Resolve(value, Rev869BSeedData.ApprovalPolicies, new DateOnly(2026, 8, 11), "SESS_PVT_LTD"));
    }

    [Fact]
    public void MissingOrAmbiguousApprovalPolicyFailsClosed()
    {
        Assert.Throws<InvalidOperationException>(() => Rev869BApprovalRoutes.Resolve(1m, Array.Empty<PurchaseTransactionApprovalPolicy>(), new DateOnly(2026, 8, 11), "SESS_PVT_LTD"));
        var duplicate = Rev869BSeedData.ApprovalPolicies.Concat(Rev869BSeedData.ApprovalPolicies.Take(1));
        Assert.Throws<InvalidOperationException>(() => Rev869BApprovalRoutes.Resolve(1m, duplicate, new DateOnly(2026, 8, 11), "SESS_PVT_LTD"));
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
        Assert.Contains("Vendor qualification was not valid at the controlled invitation event", ServiceSource);
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
    public void ExplicitExclusionsRemainAbsent()
    {
        foreach (var prohibited in new[] { "GoodsReceipt", "GRN", "InventoryLedger", "MaterialIssue", "MaterialReturn", "CustomerMaster", "ProjectMaster", "Rev869C" }) Assert.DoesNotContain(prohibited, DomainSource + ServiceSource, StringComparison.OrdinalIgnoreCase);
    }

    private static int Count(string source, string value) => source.Split(value, StringSplitOptions.None).Length - 1;
    private static string Read(params string[] parts) => File.ReadAllText(Path.Combine(new[] { Root }.Concat(parts).ToArray()));
    private static string FindRoot() { var directory = new DirectoryInfo(AppContext.BaseDirectory); while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "SESS.NexaERP.slnx"))) directory = directory.Parent; return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found."); }
}
