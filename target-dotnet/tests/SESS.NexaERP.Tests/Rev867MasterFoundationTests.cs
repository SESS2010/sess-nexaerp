using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Domain.Inventory;
using SESS.NexaERP.Domain.Masters;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed class Rev867MasterFoundationTests
{
    [Fact]
    public void Rev867_page_master_contains_required_master_pages_and_permission_rows()
    {
        var requiredPages = new[] { "masters.items", "masters.vendors", "masters.customers", "masters.warehouses", "masters.rack-bins" };
        var pageKeys = FoundationSeedData.Pages.Select(page => page.PageKey).ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.All(requiredPages, page => Assert.Contains(page, pageKeys));
        Assert.Equal(FoundationSeedData.Roles.Concat(Rev866SeedData.AdditionalEmployeeRoles).Count() * FoundationSeedData.Pages.Length, Rev866SeedData.RolePagePermissions.Count);
    }

    [Fact]
    public void Rev867_model_contains_normalized_master_support_tables()
    {
        var options = new DbContextOptionsBuilder<NexaErpDbContext>().UseNpgsql("Host=localhost;Database=test;Username=test").Options;
        using var db = new NexaErpDbContext(options);
        var tables = db.Model.GetEntityTypes().Select(e => e.GetTableName()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var table in new[] { "item_categories", "item_subcategories", "uoms", "manufacturers", "vendor_contacts", "vendor_addresses", "customer_contacts", "customer_addresses", "master_status_history", "master_approval_history", "master_attachment_metadata" })
        {
            Assert.Contains(table, tables);
        }
    }

    [Fact]
    public void Rev867_item_master_has_tracking_threshold_status_and_commercial_fields()
    {
        var item = new Item { ItemCode = "ITM-001", Name = "Compressor", DetailedDescription = "Industrial compressor", MaterialType = "Bought Out", Uom = "NOS", MinimumStock = 1, MaximumStock = 10, ReorderLevel = 3, QcRequired = true, SerialNumberTracking = true, BatchTracking = true, ShelfLifeTracking = true, StandardEstimatedPrice = 1000, Barcode = "BC-001", ImageStorageKey = "items/itm-001.png" };
        Assert.Equal(MasterStatuses.Draft, item.Status);
        Assert.Equal(MasterApprovalStatuses.Draft, item.ApprovalStatus);
        Assert.True(item.QcRequired);
        Assert.True(item.SerialNumberTracking);
        Assert.True(item.BatchTracking);
        Assert.True(item.ShelfLifeTracking);
    }

    [Theory]
    [InlineData("27ABCDE1234F1Z5", true)]
    [InlineData("ABCDE1234F", false)]
    [InlineData("BADGST", false)]
    public void Rev867_gstin_format_validation_is_available(string gstin, bool expected)
    {
        Assert.Equal(expected, SESS.NexaERP.Api.Endpoints.MasterEndpointHelpers.IsValidGstin(gstin));
    }

    [Theory]
    [InlineData("ABCDE1234F", true)]
    [InlineData("27ABCDE1234F1Z5", false)]
    [InlineData("BADPAN", false)]
    public void Rev867_pan_format_validation_is_available(string pan, bool expected)
    {
        Assert.Equal(expected, SESS.NexaERP.Api.Endpoints.MasterEndpointHelpers.IsValidPan(pan));
    }

    [Fact]
    public void Rev867_operational_roles_still_have_no_master_approval_or_commercial_power()
    {
        var restrictedRoles = new[] { "technical_engineer", "electrical_engineer", "plc_engineer", "design_engineer", "junior_engineer", "production_operator", "software_engineer", "software_developer" };
        foreach (var roleCode in restrictedRoles)
        {
            var role = Rev866SeedData.AdditionalEmployeeRoles.Single(role => role.Code == roleCode);
            var rows = Rev866SeedData.RolePagePermissions.Where(row => row.RoleId == role.Id && FoundationSeedData.Pages.Any(page => page.Id == row.PageDefinitionId && page.PageKey.StartsWith("masters.", StringComparison.OrdinalIgnoreCase))).ToList();
            Assert.NotEmpty(rows);
            Assert.All(rows, row =>
            {
                Assert.False(row.CanApprove);
                Assert.False(row.CanViewCommercialValues);
                Assert.False(row.CanExport);
                Assert.False(row.HasFullControl);
            });
        }
    }
}
