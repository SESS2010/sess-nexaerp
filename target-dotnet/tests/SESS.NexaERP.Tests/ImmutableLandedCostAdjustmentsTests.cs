using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    private const string LandedCostTarget = "20260910061358_ImmutableLandedCostAdjustments";

    [Fact]
    public void Immutable_landed_cost_applies_reverts_and_reapplies_on_disposable_postgresql()
    {
        using var model = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);
        var migrator = model.GetService<IMigrator>();
        var migrations = model.Database.GetMigrations().ToArray();
        var index = Array.IndexOf(migrations, LandedCostTarget);
        Assert.True(index > 0);
        var predecessor = migrations[index - 1];

        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("landed-cost-predecessor.sql", migrator.GenerateScript("0", predecessor));
        server.Execute("landed-cost-up.sql",
            migrator.GenerateScript(predecessor, LandedCostTarget) + LandedCostAssertions);
        server.Execute("landed-cost-down.sql",
            migrator.GenerateScript(LandedCostTarget, predecessor));
        server.Execute("landed-cost-reapply.sql",
            migrator.GenerateScript(predecessor, LandedCostTarget) + LandedCostAssertions);
    }

    [Fact]
    public void Landed_cost_contract_is_append_only_and_uses_one_allocation_basis_per_charge()
    {
        var sql = File.ReadAllText(FindLandedCostSource("src", "SESS.NexaERP.Infrastructure", "Persistence",
            "Migrations", "ImmutableLandedCostAdjustmentsSql.cs"));
        var migration = File.ReadAllText(FindLandedCostSource("src", "SESS.NexaERP.Infrastructure", "Persistence",
            "Migrations", "20260910061358_ImmutableLandedCostAdjustments.cs"));

        Assert.Equal(2, migration.Split("PostgreSqlClusterGuard.Require(migrationBuilder);",
            StringSplitOptions.None).Length - 1);
        Assert.Contains("GROSS_WEIGHT", sql);
        Assert.Contains("ITEM_VALUE", sql);
        Assert.Contains("bool_and", sql);
        Assert.Contains("line_index=line_count", sql);
        Assert.Contains("charge_row.\"ChargeValue\"-running", sql);
        Assert.Contains("ConsumedQuantityAtAcceptance", sql);
        Assert.Contains("RemainingStockAdjustmentValue", sql);
        Assert.Contains("Landed-cost evidence is immutable", sql);
        Assert.Contains("REVOKE ALL ON TABLE", sql);
        Assert.Contains("GRANT EXECUTE ON FUNCTION advance.record_vendor_bill_charges", sql);
        Assert.Contains("advance.get_actual_bom_landed_valuations(uuid,uuid)", sql);
        Assert.DoesNotContain("GRANT INSERT", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("GRANT UPDATE", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Existing Vendor Bills require an explicit landed-cost reconciliation", sql);
    }

    private const string LandedCostAssertions = """

        DO $assert$
        BEGIN
          IF to_regclass('advance.vendor_bill_charges') IS NULL
             OR to_regclass('advance.vendor_bill_charge_allocations') IS NULL
             OR to_regclass('advance.fifo_landed_cost_adjustments') IS NULL
             OR to_regclass('advance.actual_bom_valuation_adjustments') IS NULL THEN
            RAISE EXCEPTION 'Landed-cost evidence tables are missing.';
          END IF;
          IF to_regprocedure('advance.record_vendor_bill_charges(uuid,uuid,jsonb,jsonb,uuid,text,uuid,text,text)') IS NULL
             OR to_regprocedure('advance.allocate_vendor_bill_landed_cost()') IS NULL
             OR to_regprocedure('advance.get_actual_bom_landed_valuations(uuid,uuid)') IS NULL THEN
            RAISE EXCEPTION 'Controlled landed-cost functions are missing.';
          END IF;
          IF NOT EXISTS (SELECT 1 FROM pg_trigger
                         WHERE tgname='trg_aaa_vendor_bill_landed_cost')
             OR NOT EXISTS (SELECT 1 FROM pg_trigger
                            WHERE tgname='trg_fifo_landed_cost_adjustment_guard') THEN
            RAISE EXCEPTION 'Landed-cost allocation or immutability trigger is missing.';
          END IF;
          IF (SELECT count(*) FROM advance.vendor_bill_charges)<>0
             OR (SELECT count(*) FROM advance.vendor_bill_charge_allocations)<>0
             OR (SELECT count(*) FROM advance.fifo_landed_cost_adjustments)<>0
             OR (SELECT count(*) FROM advance.actual_bom_valuation_adjustments)<>0 THEN
            RAISE EXCEPTION 'Migration invented landed-cost history.';
          END IF;
          IF EXISTS (
            SELECT 1 FROM information_schema.columns
            WHERE table_schema='advance' AND table_name='actual_bom_entries'
              AND column_name='VendorBillLineId' AND is_nullable<>'YES') THEN
            RAISE EXCEPTION 'Pre-bill fitment requires a nullable bill-line reference.';
          END IF;
        END $assert$;
        """;

    private static string FindLandedCostSource(params string[] relativeParts)
    {
        var relativePath = Path.Combine(relativeParts);
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, relativePath);
            if (File.Exists(candidate)) return candidate;
            if (directory.Name.Equals("target-dotnet", StringComparison.OrdinalIgnoreCase)) break;
            directory = directory.Parent;
        }
        throw new FileNotFoundException($"Could not find {relativePath}.");
    }}