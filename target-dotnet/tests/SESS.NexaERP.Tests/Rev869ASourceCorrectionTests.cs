using SESS.NexaERP.Domain.Authorization;
using SESS.NexaERP.Domain.Masters;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed class Rev869ASourceCorrectionTests
{
    private static readonly string Root = FindRoot();
    private static string Read(params string[] parts) => File.ReadAllText(Path.Combine([Root, .. parts]));

    [Fact]
    public void CurrentUserRequiresExactResolvedIssuerSubjectIdentity()
    {
        var currentUser = Read("src", "SESS.NexaERP.Api", "Security", "ClaimsCurrentUser.cs");
        var middleware = Read("src", "SESS.NexaERP.Api", "Middleware", "EmployeeIdentityResolutionMiddleware.cs");
        var resolver = Read("src", "SESS.NexaERP.Infrastructure", "Identity", "EfEmployeeIdentityResolver.cs");
        var productionResolver = resolver[..resolver.IndexOf("#if DEBUG", StringComparison.Ordinal)];

        Assert.Contains("EmployeeIdentityResolutionMiddleware.ResolutionItemKey", currentUser);
        Assert.Contains("FindFirstValue(\"iss\")", middleware);
        Assert.Contains("FindFirstValue(\"sub\")", middleware);
        Assert.Contains("mappings.Count != 1", productionResolver);
        Assert.Contains("MasterStatuses.Active", productionResolver);
        Assert.DoesNotContain("ClaimTypes.Email", currentUser);
        Assert.DoesNotContain("ClaimTypes.Name", currentUser);
        Assert.DoesNotContain("EmployeeCode ==", productionResolver);
    }

    [Fact]
    public void MissingIdentityScopeAndDirectRecordAccessFailClosed()
    {
        var filter = Read("src", "SESS.NexaERP.Api", "Security", "EmployeeScopeEndpointFilter.cs");
        var pageFilter = Read("src", "SESS.NexaERP.Api", "Security", "PagePermissionEndpointFilter.cs");
        var pr = Read("src", "SESS.NexaERP.Api", "Endpoints", "PurchaseRequisitionEndpointHelpers.cs");
        var endpoints = Read("src", "SESS.NexaERP.Api", "Endpoints", "PurchaseRequisitionEndpoints.cs");

        Assert.Contains("!user.EmployeeId.HasValue", filter);
        Assert.Contains("AuthorizeAnyAsync", filter);
        Assert.Contains("EmployeeScopeEndpointFilter", pageFilter);
        Assert.Contains("return query.Where(_ => false)", pr);
        Assert.Contains("scope.OwnRecordsOnly", pr);
        Assert.Contains("Scope(IncludeDetail", endpoints);
        Assert.Contains("History(db, prNumber, user", endpoints);
    }

    [Fact]
    public void WarehouseRackConditionAndReservationAreFailClosed()
    {
        var model = Read("src", "SESS.NexaERP.Infrastructure", "Persistence", "NexaErpDbContext.Rev869A.cs");
        var stock = Read("src", "SESS.NexaERP.Api", "Endpoints", "PurchaseRequisitionSupport.cs");

        Assert.Contains("AK_rack_bins_WarehouseId_Id", model);
        Assert.Contains("new { x.WarehouseId, x.RackBinId }", model);
        Assert.Contains("ConditionCode == InventoryConditionCodes.Available", stock);
        Assert.Contains("x.EffectiveFrom <= today", stock);
        Assert.Contains("active AVAILABLE warehouse/RackBin condition mapping was not found", stock);
        Assert.Contains("a physical Rack/Bin is required", stock);
    }


    [Fact]
    public void GstIsStateAwareAndMissingOrOverlappingRulesFailClosed()
    {
        Assert.Equal("INTRASTATE", TaxGstSetting.ResolveSupplyType("27", "27"));
        Assert.Equal("INTERSTATE", TaxGstSetting.ResolveSupplyType("27", "29"));
        var intrastate = new TaxGstSetting { SupplyType = "INTRASTATE", GstRate = 18, CgstRate = 9, SgstRate = 9 };
        var interstate = new TaxGstSetting { SupplyType = "INTERSTATE", GstRate = 18, IgstRate = 18 };
        Assert.True(intrastate.HasValidIndiaComponentSplit());
        Assert.True(interstate.HasValidIndiaComponentSplit());

        var service = Read("src", "SESS.NexaERP.Infrastructure", "Masters", "EfRev869AFoundationServices.cs");
        var taxWorkflow = Read("src", "SESS.NexaERP.Infrastructure", "Masters", "EfTaxGstWorkflowService.cs");
        Assert.Contains("matches.Count != 1", service);
        Assert.Contains("SupplierStateCode", taxWorkflow);
        Assert.Contains("PlaceOfSupplyStateCode", taxWorkflow);
        Assert.Contains("An overlapping effective approved tax rule exists", taxWorkflow);
    }


    private static string Migration() => Read("src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations", "20260810120000_Rev869AIdentityMasterScopeFoundation.cs");
    private static int Count(string value, string needle) => (value.Length - value.Replace(needle, string.Empty, StringComparison.Ordinal).Length) / needle.Length;

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "SESS.NexaERP.slnx"))) directory = directory.Parent;
        return directory?.FullName ?? throw new InvalidOperationException("Repository root was not found.");
    }
}
