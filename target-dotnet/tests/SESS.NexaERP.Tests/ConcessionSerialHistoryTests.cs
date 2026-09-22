using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    private const string ConcessionHistoryMigration = "20260913050000_ConcessionSerialDecisionHistory";
    private const string ConcessionHistoryPredecessor = "20260913040000_StockConditionBalanceGuard";

    private static async Task AssertConcessionHistoryMigration(
        DbContextOptions<NexaErpDbContext> options, IMigrator migrator, Action<string,string> execute)
    {
        var before = await ConcessionHistoryMetadata(options);
        AssertConcessionHistoryMetadata(before, true);
        execute("concession-history-down.sql", migrator.GenerateScript(ConcessionHistoryMigration, ConcessionHistoryPredecessor));
        var down = await ConcessionHistoryMetadata(options);
        AssertConcessionHistoryMetadata(down, false);
        execute("concession-history-reapply.sql", migrator.GenerateScript(ConcessionHistoryPredecessor, ConcessionHistoryMigration));
        var after = await ConcessionHistoryMetadata(options);
        Assert.Equal(before, after);
        await SaveConcessionHistoryEvidence("migration", new { Before = before, Down = down, Reapplied = after });
    }

    private static Task<string> ConcessionHistoryMetadata(DbContextOptions<NexaErpDbContext> options) =>
        Query(options, db => db.Database.SqlQueryRaw<string>("""
            SELECT jsonb_build_object(
              'lookupUnique',i.indisunique,
              'allocationUnique',EXISTS(SELECT 1 FROM pg_index x
                WHERE x.indexrelid='advance."IX_inventory_concession_allocation_serials_CompanyId_Inventor~1"'::regclass
                  AND x.indisunique AND x.indisvalid),
              'functionPresent',p.oid IS NOT NULL,'owner',r.rolname,'securityDefiner',p.prosecdef,
              'configuration',to_jsonb(p.proconfig),'acl',coalesce(array_to_string(p.proacl,','),''),
              'runtimeExecute',CASE WHEN p.oid IS NULL THEN false
                ELSE has_function_privilege('nexa_erp_runtime',p.oid,'EXECUTE') END,
              'triggerPresent',EXISTS(SELECT 1 FROM pg_trigger t
                WHERE t.tgrelid=i.indrelid AND t.tgname='TR_concession_serial_active_allocation'
                  AND t.tgfoid=p.oid AND t.tgtype=23 AND t.tgenabled='O')
            )::text AS "Value"
            FROM pg_index i
            LEFT JOIN pg_proc p ON p.oid=to_regprocedure('advance.guard_active_concession_serial()')
            LEFT JOIN pg_roles r ON r.oid=p.proowner
            WHERE i.indexrelid='advance."IX_inventory_concession_allocation_serials_CompanyId_Inventory~"'::regclass
            """).SingleAsync());

    private static void AssertConcessionHistoryMetadata(string value, bool installed)
    {
        using var document = JsonDocument.Parse(value);
        var root = document.RootElement;
        Assert.Equal(!installed, root.GetProperty("lookupUnique").GetBoolean());
        Assert.True(root.GetProperty("allocationUnique").GetBoolean());
        Assert.Equal(installed, root.GetProperty("functionPresent").GetBoolean());
        Assert.Equal(installed, root.GetProperty("triggerPresent").GetBoolean());
        Assert.False(root.GetProperty("runtimeExecute").GetBoolean());
        if (installed)
        {
            Assert.Equal("nexa_erp_owner", root.GetProperty("owner").GetString());
            Assert.False(root.GetProperty("securityDefiner").GetBoolean());
            Assert.Equal("search_path=pg_catalog, advance",
                Assert.Single(root.GetProperty("configuration").EnumerateArray()).GetString());
        }
    }

    private static async Task AssertActiveConcessionSerialGuard(string runtimeConnection, Guid originalId)
    {
        // Deliberately bypass the API quantity check to exercise the database guard.
        // All cloned fixture rows are in one transaction that is rolled back.
        await using var connection = new NpgsqlConnection(runtimeConnection);
        await connection.OpenAsync();
        var newConcession = Guid.NewGuid();
        var newAllocation = Guid.NewGuid();
        var newSerial = Guid.NewGuid();
        await using var transaction = await connection.BeginTransactionAsync();
        await using (var copy = new NpgsqlCommand("""
            INSERT INTO advance.inventory_concessions
              SELECT (jsonb_populate_record(NULL::advance.inventory_concessions,
                to_jsonb(c)||jsonb_build_object('Id',@concession,'ConcessionNumber','GUARD-'||@concession::text,
                  'IdempotencyKey','guard-'||@concession::text))).*
              FROM advance.inventory_concessions c WHERE c."Id"=@original;
            INSERT INTO advance.inventory_concession_allocations
              SELECT (jsonb_populate_record(NULL::advance.inventory_concession_allocations,
                to_jsonb(a)||jsonb_build_object('Id',@allocation,'InventoryConcessionId',@concession))).*
              FROM advance.inventory_concession_allocations a WHERE a."InventoryConcessionId"=@original;
            """, connection, transaction))
        {
            copy.Parameters.AddWithValue("concession", newConcession);
            copy.Parameters.AddWithValue("allocation", newAllocation);
            copy.Parameters.AddWithValue("original", originalId);
            Assert.Equal(2, await copy.ExecuteNonQueryAsync());
        }
        await using var conflict = new NpgsqlCommand("""
            INSERT INTO advance.inventory_concession_allocation_serials
              SELECT (jsonb_populate_record(NULL::advance.inventory_concession_allocation_serials,
                to_jsonb(s)||jsonb_build_object('Id',@serial,'InventoryConcessionAllocationId',@allocation))).*
              FROM advance.inventory_concession_allocation_serials s
              JOIN advance.inventory_concession_allocations a ON a."Id"=s."InventoryConcessionAllocationId"
              WHERE a."InventoryConcessionId"=@original;
            """, connection, transaction);
        conflict.Parameters.AddWithValue("serial", newSerial);
        conflict.Parameters.AddWithValue("allocation", newAllocation);
        conflict.Parameters.AddWithValue("original", originalId);
        var error = await Assert.ThrowsAsync<PostgresException>(() => conflict.ExecuteNonQueryAsync());
        Assert.Equal(PostgresErrorCodes.UniqueViolation, error.SqlState);
        Assert.Equal("UX_concession_active_serial_allocation", error.ConstraintName);
        await transaction.RollbackAsync();
        await using var leftovers = new NpgsqlCommand("""
            SELECT (SELECT count(*) FROM advance.inventory_concessions WHERE "Id"=@concession)
              +(SELECT count(*) FROM advance.inventory_concession_allocations WHERE "Id"=@allocation)
              +(SELECT count(*) FROM advance.inventory_concession_allocation_serials WHERE "Id"=@serial)
            """, connection);
        leftovers.Parameters.AddWithValue("concession", newConcession);
        leftovers.Parameters.AddWithValue("allocation", newAllocation);
        leftovers.Parameters.AddWithValue("serial", newSerial);
        Assert.Equal(0L, (long)(await leftovers.ExecuteScalarAsync())!);
        await SaveConcessionHistoryEvidence("active-duplicate", new
        { error.SqlState, error.ConstraintName, error.MessageText, RemainingFixtureRows = 0 });
    }

    private static async Task AssertConcessionHistoryRollbackRefused(
        DbContextOptions<NexaErpDbContext> options, IMigrator migrator)
    {
        var history = await Query(options, db => db.Database.SqlQueryRaw<string>("""
            SELECT jsonb_agg(jsonb_build_object('SerialId',s."InventorySerialId",'Status',c."Status",
              'ConcessionId',c."Id",'RevisionId',c."QcInspectionRevisionId")
              ORDER BY c."Status")::text AS "Value"
            FROM advance.inventory_concession_allocation_serials s
            JOIN advance.inventory_concession_allocations a ON a."Id"=s."InventoryConcessionAllocationId"
            JOIN advance.inventory_concessions c ON c."Id"=a."InventoryConcessionId"
            """).SingleAsync());
        using (var document = JsonDocument.Parse(history))
        {
            var rows = document.RootElement.EnumerateArray().ToArray();
            Assert.Equal(2, rows.Length);
            Assert.Equal(new[] { "APPROVED", "REJECTED" }, rows.Select(row => row.GetProperty("Status").GetString()));
            Assert.Single(rows.Select(row => row.GetProperty("SerialId").GetGuid()).Distinct());
            Assert.Equal(2, rows.Select(row => row.GetProperty("RevisionId").GetGuid()).Distinct().Count());
        }
        var before = await ConcessionHistoryMetadata(options);
        await using var db = new NexaErpDbContext(options);
        await using var connection = new NpgsqlConnection(db.Database.GetConnectionString());
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await using var down = new NpgsqlCommand(migrator.GenerateScript(
            ConcessionHistoryMigration, ConcessionHistoryPredecessor, MigrationsSqlGenerationOptions.NoTransactions),
            connection, transaction);
        var error = await Assert.ThrowsAsync<PostgresException>(() => down.ExecuteNonQueryAsync());
        Assert.Equal(PostgresErrorCodes.RaiseException, error.SqlState);
        Assert.Equal("Concession serial history prevents rollback; historical records must not be deleted.", error.MessageText);
        await transaction.RollbackAsync();
        Assert.Equal(before, await ConcessionHistoryMetadata(options));
        Assert.Equal(2, await db.InventoryConcessionAllocationSerials.CountAsync());
        await SaveConcessionHistoryEvidence("rollback-refusal", new { History = history, error.SqlState, error.MessageText });
    }

    private static async Task SaveConcessionHistoryEvidence(string name, object value)
    {
        var directory = Path.Combine(FindRepositoryRoot(), "local-evidence", "item25");
        Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(Path.Combine(directory, $"concession-history-{name}.json"),
            JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = true }));
    }
}
