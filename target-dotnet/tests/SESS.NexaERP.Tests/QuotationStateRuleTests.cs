using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SESS.NexaERP.Application.Stores;
using SESS.NexaERP.Infrastructure.Persistence;
using SESS.NexaERP.Infrastructure.Purchase;

namespace SESS.NexaERP.Tests;

/// <summary>
/// R2 (26 Sep): supplier state = vendor GSTIN prefix, else vendor state code; place of supply = delivery
/// warehouse state, else company profile state. Typed values that differ are refused per line.
/// </summary>
public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    [Theory]
    [InlineData("27AAAAA0000A1Z5", "33", "27", QuotationStateRule.VendorGstin)]
    [InlineData(" 27aaaaa0000a1z5 ", null, "27", QuotationStateRule.VendorGstin)]
    [InlineData(null, "33", "33", QuotationStateRule.VendorStateCode)]
    [InlineData("URP", "29", "29", QuotationStateRule.VendorStateCode)]
    public void The_supplier_state_comes_from_the_GSTIN_first(string? gstin, string? stateCode, string expected, string source) =>
        Assert.Equal((expected, source), QuotationStateRule.SupplierState(gstin, stateCode));

    [Theory]
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData("URP", "TN")]
    public void A_vendor_without_a_usable_state_has_no_supplier_state(string? gstin, string? stateCode) =>
        Assert.Null(QuotationStateRule.SupplierState(gstin, stateCode));

    [Fact]
    public void Typed_states_that_differ_are_refused_naming_each_line()
    {
        var derived = new QuotationStateRule.DerivedStates("29", QuotationStateRule.VendorStateCode, "33", QuotationStateRule.Company);
        Assert.Equal("INTERSTATE", derived.SupplyType);
        QuotationStateRule.RequireMatches([(1, " 29 ", "33")], derived);
        var failure = Assert.Throws<StoresValidationException>(() => QuotationStateRule.RequireMatches([(1, "29", "33"), (2, "33", "33"), (3, "29", null)], derived));
        Assert.Equal(["Lines[2].SupplierStateCode", "Lines[3].PlaceOfSupplyStateCode"], failure.Errors!.Keys.Order().ToArray());
        Assert.Contains("Line 2: SupplierStateCode must be 29 (from the vendor's state code).", failure.Message);
        Assert.Contains("Line 3: PlaceOfSupplyStateCode must be 33 (from the company profile).", failure.Message);
    }

    [Fact]
    public async Task The_states_are_derived_from_vendor_warehouse_and_company_profile()
    {
        using var model = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);
        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("gst-states.sql", model.GetService<IMigrator>().GenerateScript("0", model.Database.GetMigrations().Last()));
        server.Execute("gst-states-trial.sql", "\\set expected_database advance_parser\n" +
            File.ReadAllText(Path.Combine(FindRepositoryRoot(), "database", "postgresql", "trial-master-data-apply.sql")));
        await using var db = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>().UseNpgsql(server.ConnectionString).Options);
        var company = Guid.Parse("70000000-0000-0000-0000-000000000001");
        var karnataka = await db.Vendors.Where(x => x.VendorCode == "TRIAL-VEN-001").Select(x => x.Id).SingleAsync();
        var tamilNadu = await db.Vendors.Where(x => x.VendorCode == "TRIAL-VEN-002").Select(x => x.Id).SingleAsync();
        var warehouse = await db.Warehouses.Where(x => x.WarehouseCode == "TRIAL-WH-C01").Select(x => x.Id).SingleAsync();

        var missing = await Assert.ThrowsAsync<StoresConflictException>(() => QuotationStateRule.DeriveAsync(db, company, karnataka, warehouse, CancellationToken.None));
        Assert.Contains("company profile has no state code", missing.Message);

        server.Execute("gst-states-profile.sql", """
            INSERT INTO advance.company_profiles("CompanyId","LegalName","Gstin","Pan","StateCode","State","AddressLine1","City","PinCode","UpdatedBy")
            VALUES ('70000000-0000-0000-0000-000000000001','SESS witness company','33ABACS5491H1ZA','ABACS5491H','33','Tamil Nadu','Witness address','Chennai','600001','TEST');
            """);
        Assert.Equal(new("29", QuotationStateRule.VendorStateCode, "33", QuotationStateRule.Company),
            await QuotationStateRule.DeriveAsync(db, company, karnataka, warehouse, CancellationToken.None));
        Assert.Equal("INTRASTATE", (await QuotationStateRule.DeriveAsync(db, company, tamilNadu, null, CancellationToken.None)).SupplyType);

        // A Karnataka delivery warehouse makes the Karnataka vendor intrastate; the GSTIN outranks the typed state code.
        server.Execute("gst-states-warehouse.sql", $"""
            INSERT INTO advance.warehouse_state_codes("WarehouseId","CompanyId","StateCode","UpdatedBy") VALUES ('{warehouse}','{company}','29','TEST');
            UPDATE advance.vendors SET "GstNumber"='27AAAAA0000A1Z5' WHERE "VendorCode"='TRIAL-VEN-002';
            """);
        var derived = await QuotationStateRule.DeriveAsync(db, company, karnataka, warehouse, CancellationToken.None);
        Assert.Equal(("29", QuotationStateRule.DeliveryWarehouse, "INTRASTATE"), (derived.PlaceOfSupplyStateCode, derived.PlaceOfSupplySource, derived.SupplyType));
        Assert.Equal(("27", QuotationStateRule.VendorGstin, "33", QuotationStateRule.Company),
            await QuotationStateRule.DeriveAsync(db, company, tamilNadu, null, CancellationToken.None) is var s ? (s.SupplierStateCode, s.SupplierStateSource, s.PlaceOfSupplyStateCode, s.PlaceOfSupplySource) : default);
    }
}
