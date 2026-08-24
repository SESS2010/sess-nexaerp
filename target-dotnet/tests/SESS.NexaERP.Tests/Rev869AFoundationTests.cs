using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using SESS.NexaERP.Domain.Authorization;
using SESS.NexaERP.Domain.Inventory;
using SESS.NexaERP.Domain.Masters;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed class Rev869AFoundationTests
{
    private static readonly DateOnly Today = new(2026, 8, 10);

    [Fact]
    public void ApprovedRoleCodesArePermissionGroupsNotLoginIds()
    {
        Assert.Equal(9, Rev869ARoleCodes.All.Distinct(StringComparer.Ordinal).Count());
        Assert.Contains(Rev869ARoleCodes.PurchaseExecutive, Rev869ARoleCodes.All);
        Assert.Contains(Rev869ARoleCodes.StoresExecutive, Rev869ARoleCodes.All);
        Assert.Contains(Rev869ARoleCodes.QcInspector, Rev869ARoleCodes.All);
        Assert.True(Rev869ARoleCodes.IsExplicitCrossScopeRole("technical_director"));
        Assert.False(Rev869ARoleCodes.IsExplicitCrossScopeRole("purchase_manager"));
        Assert.Equal((8 * 8) + 2, Rev869ASeedData.RolePagePermissions.Count);
        Assert.Equal(4, Rev869ASeedData.Roles.Length);
        Assert.DoesNotContain(Rev869ASeedData.Roles, x => x.Code == Rev869ARoleCodes.DepartmentManager);
    }

    [Fact]
    public void OperationalScopeUsesTheMostRestrictiveIntersection()
    {
        var department = Guid.NewGuid();
        var warehouse = Guid.NewGuid();
        var rackBin = Guid.NewGuid();
        var scope = new EmployeeOperationalScope
        {
            DepartmentId = department,
            WarehouseId = warehouse,
            RackBinId = rackBin,
            EffectiveFrom = Today,
            IsActive = true
        };

        Assert.True(scope.Matches(department, warehouse, rackBin, Today));
        Assert.False(scope.Matches(Guid.NewGuid(), warehouse, rackBin, Today));
        Assert.False(scope.Matches(department, Guid.NewGuid(), rackBin, Today));
        Assert.False(scope.Matches(department, warehouse, Guid.NewGuid(), Today));
        scope.IsActive = false;
        Assert.False(scope.Matches(department, warehouse, rackBin, Today));
    }

    [Fact]
    public void UomConversionsAreDimensionControlledPreciseAndImmutableAfterUse()
    {
        var from = Guid.NewGuid();
        var to = Guid.NewGuid();
        Assert.True(UomConversion.IsValid(0.000001m, 6, from, to, "MASS"));
        Assert.False(UomConversion.IsValid(0, 6, from, to, "MASS"));
        Assert.False(UomConversion.IsValid(1, 5, from, to, "MASS"));
        Assert.False(UomConversion.IsValid(1, 6, from, from, "MASS"));

        var conversion = new UomConversion();
        Assert.True(UomConversion.CanEdit(conversion));
        conversion.FirstUsedAt = DateTimeOffset.UtcNow;
        Assert.False(UomConversion.CanEdit(conversion));
    }

    [Fact]
    public void TaxAndCommercialValuesAreEffectiveDatedAndAuditableByComponent()
    {
        Assert.True(TaxGstSetting.IsValidRange(Today, Today));
        Assert.False(TaxGstSetting.IsValidRange(Today, Today.AddDays(-1)));
        Assert.True(TaxGstSetting.IsValidRate(28));
        Assert.False(TaxGstSetting.IsValidRate(101));

        var values = CommercialValueSnapshot.Calculate("inr", 100m, 18m, 7.125m, 2m);
        Assert.Equal("INR", values.CurrencyCode);
        Assert.Equal(123.13m, values.TotalPayableValue);
        Assert.Equal(100m, values.TaxableValue);
    }

    [Fact]
    public void VendorMustBeActiveApprovedVerifiedAndEffective()
    {
        var vendor = new Vendor
        {
            IsActive = true,
            VendorStatus = MasterStatuses.Active,
            ApprovalStatus = MasterApprovalStatuses.Approved,
            CommercialVerificationStatus = MasterApprovalStatuses.Approved,
            EffectiveFrom = Today
        };
        Assert.True(VendorQualification.IsVendorEligible(vendor, Today));
        vendor.RequiresReverification = true;
        vendor.CommercialVerificationStatus = MasterApprovalStatuses.Draft;
        vendor.ApprovalStatus = MasterApprovalStatuses.Draft;
        Assert.False(VendorQualification.IsVendorEligible(vendor, Today));

        var finalApprover = Rev869ASeedData.OrganizationPolicies.Single(x => x.PolicyCode == Rev869APolicyCodes.VendorFinalApprover);
        Assert.Equal(Rev869ARoleCodes.ManagingDirector, finalApprover.PolicyValue);
        Assert.DoesNotContain("EMPLOYEE", finalApprover.PolicyValue, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void OnlyAvailableStockCanBeReservedOrIssuedAndMissingQcFailsClosed()
    {
        Assert.True(InventoryConditionCodes.CanReserveOrIssue(InventoryConditionCodes.Available));
        foreach (var condition in InventoryConditionCodes.All.Where(x => x != InventoryConditionCodes.Available))
            Assert.False(InventoryConditionCodes.CanReserveOrIssue(condition));
        Assert.Equal(InventoryConditionCodes.QcHold, QcInspectionPolicy.ResolveMissingPolicyCondition(null, Today));
    }

    [Fact]
    public void WarehouseRackBinMappingAndDerivedLocationCannotCompeteWithWorkLocation()
    {
        var warehouseId = Guid.NewGuid();
        var rackBinId = Guid.NewGuid();
        var rackBin = new RackBin { Id = rackBinId, WarehouseId = warehouseId, MaterialCondition = InventoryConditionCodes.Available };
        var mapping = new WarehouseConditionLocation { WarehouseId = warehouseId, RackBinId = rackBinId, ConditionCode = InventoryConditionCodes.Available };
        Assert.True(WarehouseConditionLocation.IsValid(mapping, rackBin));
        mapping.WarehouseId = Guid.NewGuid();
        Assert.False(WarehouseConditionLocation.IsValid(mapping, rackBin));
        Assert.StartsWith($"W:{warehouseId:N}:B:", StoreLocationKey.Derive(warehouseId, rackBinId), StringComparison.Ordinal);
    }

    [Fact]
    public void Rev869AMigrationIsDiscoverableAndContainsNoBaselineDrift()
    {
        var migrations = typeof(NexaErpDbContext).Assembly.GetTypes()
            .Select(type => (Type: type, Attribute: type.GetCustomAttribute<MigrationAttribute>()))
            .Where(x => x.Attribute is not null)
            .ToArray();
        var migration = Assert.Single(migrations, migration =>
            migration.Attribute!.Id == migrations.Min(x => x.Attribute!.Id));
        Assert.Equal("20260824032638_AdvanceInitialBaseline", migration.Attribute!.Id);

        var source = File.ReadAllText(Path.Combine(RepositoryRoot(), "src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations", migration.Attribute.Id + ".cs"));
        Assert.Contains("employee_identity_mappings", source, StringComparison.Ordinal);
        Assert.Contains("uom_conversions", source, StringComparison.Ordinal);
        Assert.Contains("tax_gst_settings", source, StringComparison.Ordinal);
        Assert.Contains("controlled_configuration_histories", source, StringComparison.Ordinal);

        var options = new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=advance_no_connect;Username=no_connect")
            .Options;
        using var db = new NexaErpDbContext(options);
        var snapshotType = typeof(NexaErpDbContext).Assembly.GetType("SESS.NexaERP.Infrastructure.Persistence.Migrations.NexaErpDbContextModelSnapshot", throwOnError: true)!;
        var snapshot = (ModelSnapshot)Activator.CreateInstance(snapshotType, nonPublic: true)!;
        var current = db.GetService<IDesignTimeModel>().Model;
        var differ = db.GetService<IMigrationsModelDiffer>();
        var initializedSnapshot = db.GetService<IModelRuntimeInitializer>().Initialize(snapshot.Model, designTime: true);
        Assert.Empty(differ.GetDifferences(initializedSnapshot.GetRelationalModel(), current.GetRelationalModel()));
    }

    [Fact]
    public void IdentityResolutionAndApprovalRoutingHaveNoPersonOrEmailFallback()
    {
        var root = RepositoryRoot();
        var resolver = File.ReadAllText(Path.Combine(root, "src", "SESS.NexaERP.Infrastructure", "Identity", "EfEmployeeIdentityResolver.cs"));
        var routing = File.ReadAllText(Path.Combine(root, "src", "SESS.NexaERP.Api", "Endpoints", "PurchaseRequisitionEndpointHelpers.cs"));
        Assert.DoesNotContain("x.Email", resolver, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("x.Name", resolver, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Issuer", resolver, StringComparison.Ordinal);
        Assert.Contains("Subject", resolver, StringComparison.Ordinal);
        Assert.DoesNotContain("SESS-001", routing, StringComparison.Ordinal);
        Assert.DoesNotContain("SESS-002", routing, StringComparison.Ordinal);
    }

    private static string RepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "SESS.NexaERP.slnx"))) return directory.FullName;
        throw new DirectoryNotFoundException("Repository root was not found.");
    }
}
