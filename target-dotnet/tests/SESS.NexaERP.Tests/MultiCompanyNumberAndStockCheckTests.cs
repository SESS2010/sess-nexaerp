using Microsoft.EntityFrameworkCore;
using SESS.NexaERP.Domain.Purchase;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed class MultiCompanyNumberAndStockCheckTests
{
    [Fact]
    public void PurchaseRequisitionNumberIsUniqueWithinCompanyNotGlobally()
    {
        using var db = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect")
            .Options);
        var entity = db.Model.FindEntityType(typeof(PurchaseRequisition))!;
        var numberIndex = entity.GetIndexes().Single(x =>
            x.Properties.Select(p => p.Name).SequenceEqual(["CompanyId", "PrNumber"]));

        Assert.True(numberIndex.IsUnique);
        Assert.DoesNotContain(entity.GetIndexes(), x =>
            x.IsUnique && x.Properties.Select(p => p.Name).SequenceEqual(["PrNumber"]));
    }

    [Fact]
    public void EveryOtherGeneratedPurchaseStoresAndQcNumberIsAlreadyScopeUnique()
    {
        var purchase = Read("src", "SESS.NexaERP.Infrastructure", "Persistence", "NexaErpDbContext.Rev869B.cs");
        foreach (var field in new[] { "x.RfqNumber", "x.QuotationNumber", "x.ComparisonNumber", "x.PoNumber" })
            Assert.Contains("x.OrganizationId, " + field, purchase);

        var stores1 = Read("src", "SESS.NexaERP.Infrastructure", "Persistence", "NexaErpDbContext.StoresPart1.cs");
        var stores2 = Read("src", "SESS.NexaERP.Infrastructure", "Persistence", "NexaErpDbContext.StoresPart2.cs");
        var stores3 = Read("src", "SESS.NexaERP.Infrastructure", "Persistence", "NexaErpDbContext.StoresPart3A.cs");
        var provenance = Read("src", "SESS.NexaERP.Infrastructure", "Persistence", "NexaErpDbContext.InventoryProvenanceGenealogy.cs");
        Assert.Contains("x.CompanyId, x.GateEntryNumber", stores1);
        Assert.Contains("x.CompanyId, x.GrnNumber", stores2);
        Assert.Contains("x.CompanyId, x.InspectionNumber", stores3);
        Assert.Contains("x.CompanyId, x.ConcessionNumber", provenance);
    }

    [Fact]
    public void StoresStockCheckProjectionUsesVerifyPermissionAndPendingStatusOnly()
    {
        var source = Read("src", "SESS.NexaERP.Api", "Endpoints", "PurchaseRequisitionEndpoints.cs");
        Assert.Contains("/api/v1/stores/stock-check", source);
        Assert.Contains("stockCheckGroup.MapGet", source);
        Assert.Contains("PageStockCheck, PagePermissionActions.Verify", source);
        Assert.Contains("x.Status == PurchaseRequisitionStatuses.StockCheckPending", source);
        Assert.DoesNotContain("PageStockCheck, PagePermissionActions.View", source);
    }

    [Fact]
    public void NamedGrantRecipientsAreExplicitInTheAuthoritativeRoleMatrix()
    {
        var roleMatrix = Read("src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations", "MultiCompanyEmployeeAuthorizationPart1Data.cs");
        Assert.Contains("""R("SESS-35", "STORES_EXECUTIVE")""", roleMatrix);
        Assert.Contains("""R("SESS-15", "STORES_EXECUTIVE")""", roleMatrix);
        Assert.Contains("""R("SESS-16", "STORES_ASSISTANT")""", roleMatrix);
        Assert.Contains("""R("SESS-41", "ACCOUNTS_ASSISTANT")""", roleMatrix);

        var grnSql = Read("src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations", "StoresGrnSlice2Sql.cs");
        Assert.Contains("SESS-41", grnSql);
        Assert.Contains("STORES_ASSISTANT", grnSql);
    }

    [Fact]
    public void CorrectionMigrationIsClusterGuardedAndUpdatesExactlyThreePermissionRows()
    {
        var migration = Read("src", "SESS.NexaERP.Infrastructure", "Persistence", "Migrations", "20260904075003_MultiCompanyPrAndStoresStockCheckCorrection.cs");
        Assert.Equal(2, Count(migration, "PostgreSqlClusterGuard.Require(migrationBuilder);"));
        Assert.Equal(6, Count(migration, "migrationBuilder.UpdateData("));
        Assert.Contains("IX_purchase_requisitions_CompanyId_PrNumber", migration);
        Assert.DoesNotContain("InsertData", migration);
        Assert.DoesNotContain("DeleteData", migration);
    }

    private static int Count(string source, string value) => source.Split(value, StringSplitOptions.None).Length - 1;
    private static string Read(params string[] parts) => File.ReadAllText(Path.Combine([Root, .. parts]));
    private static readonly string Root = FindRoot();
    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "SESS.NexaERP.slnx")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
    }
}
