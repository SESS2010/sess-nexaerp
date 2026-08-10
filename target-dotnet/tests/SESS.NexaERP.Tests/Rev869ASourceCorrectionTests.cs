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

        Assert.Contains("EmployeeIdentityResolutionMiddleware.ResolutionItemKey", currentUser);
        Assert.Contains("FindFirstValue(\"iss\")", middleware);
        Assert.Contains("FindFirstValue(\"sub\")", middleware);
        Assert.Contains("mappings.Count != 1", resolver);
        Assert.Contains("MasterStatuses.Active", resolver);
        Assert.DoesNotContain("ClaimTypes.Email", currentUser);
        Assert.DoesNotContain("ClaimTypes.Name", currentUser);
        Assert.DoesNotContain("EmployeeCode ==", resolver);
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
    public void UomBackfillIsExactAndNeverInventsDefault()
    {
        var migration = Migration();
        Assert.Contains("every existing item must have an exact existing UomId", migration);
        Assert.Contains("UPDATE nexa.items SET \"BaseUomId\" = \"UomId\"", migration);
        Assert.Contains("ALTER COLUMN \"BaseUomId\" SET NOT NULL", migration);
        Assert.DoesNotContain("00000000-0000-0000-0000-000000000000", migration);
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
        var endpoint = Read("src", "SESS.NexaERP.Api", "Endpoints", "Rev869AConfigurationEndpoints.cs");
        Assert.Contains("matches.Count != 1", service);
        Assert.Contains("SupplierStateCode", endpoint);
        Assert.Contains("PlaceOfSupplyStateCode", endpoint);
        Assert.Contains("An overlapping effective tax rule exists", endpoint);
    }

    [Fact]
    public void EffectiveIndexesAreNullSafeAndHistoryIsDatabaseAppendOnly()
    {
        var model = Read("src", "SESS.NexaERP.Infrastructure", "Persistence", "NexaErpDbContext.Rev869A.cs");
        var migration = Migration();
        Assert.True(Count(model, ".AreNullsDistinct(false)") >= 7);
        Assert.True(Count(migration, "Npgsql:NullsDistinct") >= 7);
        Assert.Contains("rev869a_block_history_mutation", migration);
        Assert.Contains("Controlled configuration history is append-only", migration);
        Assert.Contains("Controlled configuration versions cannot be deleted", migration);
        Assert.Contains("close the old version and insert a corrected version", migration);
    }

    [Fact]
    public void PermissionSeedsAndDownAreExactlySymmetric()
    {
        var departmentManager = Rev869ASeedData.Roles.Single(x => x.Code == Rev869ARoleCodes.DepartmentManager);
        Assert.DoesNotContain(Rev869ASeedData.RolePagePermissions, x => x.RoleId == departmentManager.Id);
        var upSeeds = Rev869ASeedData.Roles.Length + Rev869ASeedData.Pages.Length + Rev869ASeedData.OrganizationPolicies.Length + Rev869ASeedData.RolePagePermissions.Count;
        Assert.Equal(81, upSeeds);
        Assert.Equal(81, Count(Migration(), "migrationBuilder.DeleteData("));
    }

    [Fact]
    public void MigrationPreservesRev868AndExcludedBoundaries()
    {
        var migration = Migration();
        Assert.Contains("rev869a_items_prechange_backup", migration);
        Assert.Contains("DROP TABLE nexa.rev869a_items_prechange_backup", migration);
        Assert.DoesNotContain("purchase_requisitions", migration, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("stock_reservations", migration, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("department_approval_mappings", migration, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("employees SET", migration, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("project_master", migration, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("machine_master", migration, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("rfq", migration, StringComparison.OrdinalIgnoreCase);
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
