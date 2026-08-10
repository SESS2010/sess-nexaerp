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
    public void UomCreationAndItemBackfillAreExactAndManagementApproved()
    {
        var migration = Migration();
        Assert.Equal(Guid.Parse("f71a4725-bb15-e7bf-e97b-991985e96328"), Rev869ASeedData.ApprovedEaUomId);
        foreach (var value in new[] { "EA", "Each", "COUNT", "IDENTITY_ONLY", "MGMT-REV869A-UOM-20260810-001", "8c428e59-db05-471d-a7e7-4f7dc1c13b54", "REV868C1-ITEM" })
            Assert.Contains(value, migration);
        Assert.Contains("INSERT INTO nexa.uoms", migration);
        Assert.Contains("UPDATE nexa.items SET \"UomId\" = 'f71a4725-bb15-e7bf-e97b-991985e96328'::uuid, \"BaseUomId\" = 'f71a4725-bb15-e7bf-e97b-991985e96328'::uuid", migration);
        Assert.Contains("WHERE \"Id\" = '8c428e59-db05-471d-a7e7-4f7dc1c13b54'::uuid AND \"ItemCode\" = 'REV868C1-ITEM' AND \"UomId\" IS NULL", migration);
        Assert.Contains("ALTER COLUMN \"BaseUomId\" SET NOT NULL", migration);
        Assert.DoesNotContain("UPDATE nexa.items SET \"BaseUomId\" = \"UomId\"", migration);
        Assert.DoesNotContain("00000000-0000-0000-0000-000000000000", migration);
    }

    [Fact]
    public void UomRollbackRestoresNullAndDeletesOnlyOwnedEaRows()
    {
        var migration = Migration();
        Assert.Contains("UPDATE nexa.items i SET \"UomId\" = b.\"UomId\", \"BaseUomId\" = null", migration);
        Assert.Contains("rollback cannot prove the exact original null Item UomId", migration);
        Assert.Contains("DELETE FROM nexa.controlled_configuration_histories WHERE \"Id\" = '0007efa3-4888-a87d-45ef-72cc55f4dd45'::uuid", migration);
        Assert.Contains("DELETE FROM nexa.uoms WHERE \"Id\" = 'f71a4725-bb15-e7bf-e97b-991985e96328'::uuid", migration);
        Assert.Contains("\"CreatedBy\" = 'migration-rev869a'", migration);
        Assert.Contains("rollback refuses to delete EA because an unapproved Item references it", migration);
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
        Assert.Equal(4, Rev869ASeedData.Roles.Length);
        Assert.DoesNotContain(Rev869ASeedData.Roles, x => x.Code == Rev869ARoleCodes.DepartmentManager);
        Assert.Equal(66, Rev869ASeedData.RolePagePermissions.Count);
        var upSeeds = Rev869ASeedData.Roles.Length + Rev869ASeedData.Pages.Length + Rev869ASeedData.OrganizationPolicies.Length + Rev869ASeedData.RolePagePermissions.Count;
        Assert.Equal(80, upSeeds);
        Assert.Equal(80, Count(Migration(), "migrationBuilder.DeleteData("));
        Assert.Contains("INSERT INTO nexa.role_page_permissions", Migration(), StringComparison.Ordinal);
        foreach (var id in new[] { "aea2e8a1-18a6-72d2-a954-6f5513b80eeb", "f8e7d0a6-f056-175a-e604-14c1f9f6ad83", "a98dbcec-f959-9f7c-c5f7-3c3a2c8bec12", "15ee5b19-d532-c28c-b755-de4152769a7a", "5794f740-90b1-5a70-413a-d59bbc97ce78", "42e2a253-d767-6191-caf9-e1f79652c44f", "38371df3-5a46-5137-8204-4c5391633180", "680f7358-4b7c-0733-be42-f9d52e746d1b" })
            Assert.Equal(1, Count(Migration(), id));
        Assert.DoesNotContain("30000000-0000-0000-0000-000000000005", Migration(), StringComparison.Ordinal);
        Assert.DoesNotContain("\"DEPARTMENT_MANAGER\", new DateTimeOffset", Migration(), StringComparison.Ordinal);
    }

    [Fact]
    public void DepartmentManagerReuseNeverSeedsUpdatesOrDeletesTheExistingRole()
    {
        var migration = Migration();
        var designer = Read("src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations", "20260810120000_Rev869AIdentityMasterScopeFoundation.Designer.cs");
        var snapshot = Read("src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations", "NexaErpDbContextModelSnapshot.cs");
        var expectedCreatedCodes = new[] { Rev869ARoleCodes.PurchaseManager, Rev869ARoleCodes.StoresManager, Rev869ARoleCodes.QcManager, Rev869ARoleCodes.QcInspector };

        Assert.Equal(expectedCreatedCodes.Order(), Rev869ASeedData.Roles.Select(x => x.Code).Order());
        Assert.DoesNotContain(Rev869ARoleCodes.DepartmentManager, Rev869ASeedData.Roles.Select(x => x.Code));
        Assert.DoesNotContain("30000000-0000-0000-0000-000000000005", migration + designer + snapshot, StringComparison.Ordinal);
        Assert.Equal(4, Count(migration, "table: \"roles\"" ) - 1);
        Assert.Equal(66, Count(migration, "table: \"role_page_permissions\"") - 1);
        Assert.Contains("DELETE FROM nexa.role_page_permissions p", migration, StringComparison.Ordinal);
        Assert.Contains("p.\"CreatedBy\" = 'migration-rev869a'", migration, StringComparison.Ordinal);
        Assert.Contains("r.\"Code\" = 'DEPARTMENT_MANAGER'", migration, StringComparison.Ordinal);
        Assert.DoesNotContain("update nexa.roles", migration, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("requires exactly one active suitable pre-existing DEPARTMENT_MANAGER role", migration, StringComparison.Ordinal);
        Assert.Contains("REV869A new-role collision detected", migration, StringComparison.Ordinal);
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
