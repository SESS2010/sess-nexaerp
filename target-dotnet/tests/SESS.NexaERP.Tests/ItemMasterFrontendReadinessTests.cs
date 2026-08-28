using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using SESS.NexaERP.Api.Endpoints;
using SESS.NexaERP.Application.Inventory;
using SESS.NexaERP.Domain.Authorization;
using SESS.NexaERP.Domain.Inventory;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed class ItemMasterFrontendReadinessTests
{
    private const string MigrationName = "20260828121759_ItemReferenceMasterFrontendReadiness.cs";

    [Fact]
    public void Item_contract_requires_category_and_accepts_subcategory()
    {
        var properties = typeof(UpsertItemRequest).GetProperties().ToDictionary(x => x.Name);
        Assert.Equal(typeof(Guid), properties["CategoryId"].PropertyType);
        Assert.Equal(typeof(Guid?), properties["SubcategoryId"].PropertyType);
        Assert.Equal(typeof(string), properties["PreferredVendorCode"].PropertyType);
    }

    [Fact]
    public void Legacy_item_category_column_remains_nullable()
    {
        using var db = CreateContext();
        var item = db.Model.FindEntityType(typeof(Item))!;
        Assert.True(item.FindProperty(nameof(Item.CategoryId))!.IsNullable);
        Assert.True(item.FindProperty(nameof(Item.SubcategoryId))!.IsNullable);
    }

    [Fact]
    public void Three_reference_master_pages_and_permissions_are_seeded()
    {
        Assert.Equal(
            ["masters.item-categories", "masters.item-subcategories", "masters.manufacturers"],
            ItemReferenceMasterSeedData.Pages.Select(x => x.PageKey).Order().ToArray());
        Assert.Equal(129, ItemReferenceMasterSeedData.RolePagePermissions.Count);
        Assert.All(ItemReferenceMasterSeedData.Pages, page =>
            Assert.Equal(43, ItemReferenceMasterSeedData.RolePagePermissions.Count(x => x.PageDefinitionId == page.Id)));
    }

    [Fact]
    public void Reference_master_routes_include_crud_and_dependency_guards()
    {
        var source = Read("src", "SESS.NexaERP.Api", "Endpoints", "ReferenceMasterEndpoints.cs");
        foreach (var resource in new[] { "item-categories", "item-subcategories", "uoms", "manufacturers" })
        {
            Assert.Contains("/" + resource, source);
            Assert.Contains(resource + "/{id:guid}", source);
            Assert.Contains(resource + "/{id:guid}/deactivate", source);
        }
        Assert.Contains("db.StoreCategoryRoutes.AnyAsync", source);
        Assert.Contains("db.QcInspectionPolicies.AnyAsync", source);
        Assert.Contains("db.UomConversions.AnyAsync", source);
        Assert.Contains("db.DeliveryChallanLines.AnyAsync", source);
    }

    [Fact]
    public void Item_write_path_resolves_category_subcategory_and_preferred_vendor()
    {
        var source = Read("src", "SESS.NexaERP.Api", "Endpoints", "InventoryEndpoints.cs");
        Assert.Contains("item.CategoryId = category.Id", source);
        Assert.Contains("item.SubcategoryId = subcategory?.Id", source);
        Assert.Contains("item.PreferredVendorId = preferredVendor?.Id", source);
        Assert.Contains("Preferred vendor must identify one active vendor.", source);
    }

    [Fact]
    public void Vendor_bank_redaction_handles_both_wire_and_database_names_recursively()
    {
        const string json = """
            {"BankMetadata":{"Account":"123"},"Nested":{"BankMetadataJson":{"Ifsc":"ABC"}},"Safe":"kept"}
            """;
        var result = MasterEndpointHelpers.RedactVendorBankMetadata(json);
        Assert.NotNull(result);
        Assert.Contains("BankMetadata", result);
        Assert.Contains("BankMetadataJson", result);
        Assert.Contains("kept", result);
        Assert.DoesNotContain("Account", result);
        Assert.DoesNotContain("Ifsc", result);
        Assert.DoesNotContain("123", result);
        Assert.DoesNotContain("ABC", result);
        Assert.Null(MasterEndpointHelpers.RedactVendorBankMetadata("{not-json"));
    }

    [Fact]
    public void Migration_is_seed_only_and_guards_up_and_down()
    {
        var source = Read("src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations", MigrationName);
        Assert.Equal(2, Count(source, "PostgreSqlClusterGuard.Require(migrationBuilder);"));
        Assert.Equal(2, Count(source, "migrationBuilder.InsertData("));
        Assert.DoesNotContain("CreateTable(", source);
        Assert.DoesNotContain("AlterColumn(", source);
        Assert.DoesNotContain("AddColumn(", source);
        Assert.Contains("masters.item-categories", source);
        Assert.Contains("masters.item-subcategories", source);
        Assert.Contains("masters.manufacturers", source);
    }

    [Theory]
    [InlineData("Up")]
    [InlineData("Down")]
    public void Migration_guard_rejects_non_postgresql_provider_in_both_directions(string methodName)
    {
        var migration = new SESS.NexaERP.Infrastructure.Persistence.Migrations.ItemReferenceMasterFrontendReadiness();
        var method = migration.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)!;
        var error = Assert.Throws<TargetInvocationException>(() =>
            method.Invoke(migration, [new MigrationBuilder("Microsoft.EntityFrameworkCore.Sqlite")]));
        Assert.IsType<NotSupportedException>(error.InnerException);
    }

    [Fact]
    public void New_page_permissions_are_present_in_the_design_time_model()
    {
        using var db = CreateContext();
        var pageIds = ItemReferenceMasterSeedData.Pages.Select(p => p.Id).ToHashSet();
        var seeded = db.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(RolePagePermission))!
            .GetSeedData().Count(x => pageIds.Contains((Guid)x["PageDefinitionId"]!));
        Assert.Equal(129, seeded);
    }

    private static NexaErpDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect")
            .Options;
        return new NexaErpDbContext(options);
    }

    private static int Count(string text, string value) =>
        (text.Length - text.Replace(value, "", StringComparison.Ordinal).Length) / value.Length;

    private static string Read(params string[] parts)
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        return File.ReadAllText(Path.Combine([root, .. parts]));
    }
}
