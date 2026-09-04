using System.Reflection;
using Microsoft.EntityFrameworkCore.Migrations;
using SESS.NexaERP.Domain.Authorization;
using SESS.NexaERP.Infrastructure.Persistence;
using SESS.NexaERP.Infrastructure.Persistence.Migrations;

namespace SESS.NexaERP.Tests;

public sealed class RequestInputReachabilityGapCorrectionTests
{
    [Fact]
    public void GateEntryCreatePermissionHasScopedPurchaseOrderAndLineCandidates()
    {
        var endpoints = Read("src", "SESS.NexaERP.Api", "Endpoints", "StoresGateEntryEndpoints.cs");
        var contracts = Read("src", "SESS.NexaERP.Application", "Stores", "GateEntryContracts.cs");
        var queries = Read("src", "SESS.NexaERP.Infrastructure", "Stores", "EfGateEntryService.Queries.cs");

        var route = endpoints.Split(Environment.NewLine).Single(x => x.Contains("/purchase-order-candidates"));
        Assert.Contains("PagePermissionActions.Create", route);
        Assert.Contains("PurchaseOrderNumber", contracts);
        Assert.Contains("PurchaseOrderLineId", contracts);
        Assert.Contains("x.IsCurrentVersion && x.Status == Rev869BStatuses.Issued", queries);
        Assert.Contains("scopes.AuthorizeAsync", queries);
        Assert.Contains("RequireReceiptOperatorAsync(ct)", queries);
    }

    [Fact]
    public void RfqChainCandidateReadsUseThePermissionOfTheirCommands()
    {
        var endpoints = Read("src", "SESS.NexaERP.Api", "Endpoints", "Rev869BPurchaseEndpoints.cs");
        var contracts = Read("src", "SESS.NexaERP.Application", "Purchase", "Rev869BPurchaseReadContracts.cs");

        var vendorRoute = endpoints.Split(Environment.NewLine).Single(x => x.Contains("/rfqs/{number}/vendor-candidates"));
        var invitationRoute = endpoints.Split(Environment.NewLine).Single(x => x.Contains("/rfq-invitations") && x.Contains("MapGet"));
        var comparisonRoute = endpoints.Split(Environment.NewLine).Single(x => x.Contains("/comparisons/rfq-candidates"));
        Assert.Contains("purchase.rfq", vendorRoute);
        Assert.Contains("PagePermissionActions.Submit", vendorRoute);
        Assert.Contains("purchase.vendor-quotations", invitationRoute);
        Assert.Contains("PagePermissionActions.Create", invitationRoute);
        Assert.Contains("purchase.commercial-comparisons", comparisonRoute);
        Assert.Contains("PagePermissionActions.Create", comparisonRoute);
        foreach (var field in new[] { "VendorId", "InvitationId", "InvitationVersion", "RequestForQuotationLineId", "RfqNumber", "RfqVersion" })
            Assert.Contains(field, contracts);
        Assert.Contains("ScopeRfqs", endpoints);
        Assert.Contains("qualifications.IsEligibleAsync", endpoints);
        Assert.Contains("Rev869BStatuses.TechnicallyCompliant", endpoints);
    }

    [Fact]
    public void ComparisonAndMaterialFollowUpOutputsExposeMandatoryCommandInputs()
    {
        var endpoints = Read("src", "SESS.NexaERP.Api", "Endpoints", "Rev869BPurchaseEndpoints.cs");
        var contracts = Read("src", "SESS.NexaERP.Application", "Purchase", "Rev869BPurchaseReadContracts.cs");

        Assert.Equal(2, Count(endpoints, "VendorQuotationId = x.VendorQuotationLine!.VendorQuotationId"));
        Assert.Contains("ThenInclude(x => x.VendorQuotationLine)", endpoints);
        Assert.Contains("DateTimeOffset HandoffAt, uint Version", contracts);
        Assert.Contains("x.HandoffAt, x.Version", endpoints);
    }

    [Fact]
    public void MaterialFollowUpUpdateGrantMatchesTheTwoServiceRolesExactly()
    {
        var page = Rev869BSeedData.Pages.Single(x => x.PageKey == "purchase.material-followup");
        var roles = FoundationSeedData.Roles.Concat(Rev866SeedData.AdditionalEmployeeRoles).Concat(Rev869ASeedData.Roles)
            .GroupBy(x => x.Id).ToDictionary(x => x.Key, x => x.First().Code);
        var writers = Rev869BSeedData.RolePagePermissions.Where(x => x.PageDefinitionId == page.Id && x.CanUpdate)
            .Select(x => roles[x.RoleId]).Order(StringComparer.Ordinal).ToArray();
        Assert.Equal(new[] { Rev869ARoleCodes.StoresExecutive, Rev869ARoleCodes.StoresManager }.Order(StringComparer.Ordinal), writers);

        var service = Read("src", "SESS.NexaERP.Infrastructure", "Purchase", "EfRev869BPurchaseService.MaterialFollowUp.cs");
        Assert.Contains("RequireRole(Rev869ARoleCodes.StoresExecutive, Rev869ARoleCodes.StoresManager)", service);
    }

    [Theory]
    [InlineData("Up")]
    [InlineData("Down")]
    public void GrantMigrationIsPostgresqlGuardedAndUpdatesExactlyTwoRowsEachWay(string methodName)
    {
        var migration = new StoresMaterialFollowUpUpdateGrant();
        var method = migration.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)!;
        var error = Assert.Throws<TargetInvocationException>(() => method.Invoke(migration, [new MigrationBuilder("Microsoft.EntityFrameworkCore.Sqlite")]));
        Assert.IsType<NotSupportedException>(error.InnerException);

        var source = Read("src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations", "20260904061416_StoresMaterialFollowUpUpdateGrant.cs");
        Assert.Equal(2, Count(source, "PostgreSqlClusterGuard.Require(migrationBuilder);"));
        Assert.Equal(4, Count(source, "migrationBuilder.UpdateData("));
        Assert.DoesNotContain("InsertData", source);
        Assert.DoesNotContain("DeleteData", source);
    }

    private static int Count(string text, string value) =>
        (text.Length - text.Replace(value, string.Empty, StringComparison.Ordinal).Length) / value.Length;
    private static string Read(params string[] parts) => File.ReadAllText(Path.Combine([Root, .. parts]));
    private static readonly string Root = FindRoot();
    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "SESS.NexaERP.slnx"))) directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
    }
}
