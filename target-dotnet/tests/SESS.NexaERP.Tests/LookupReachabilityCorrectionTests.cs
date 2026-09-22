using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    private const string LookupReachabilityTarget = "20260910164750_LookupReachabilityCorrections";

#if MIGRATION_LIFECYCLE_WITNESS
    [Fact]
    public void Lookup_reachability_grants_apply_revert_and_reapply_on_disposable_postgresql()
    {
        using var model = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);
        var migrator = model.GetService<IMigrator>();
        var migrations = model.Database.GetMigrations().ToArray();
        var index = Array.IndexOf(migrations, LookupReachabilityTarget);
        Assert.True(index > 0);
        var predecessor = migrations[index - 1];
        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("lookup-reachability-predecessor.sql", migrator.GenerateScript("0", predecessor));
        server.Execute("lookup-reachability-up.sql", migrator.GenerateScript(predecessor, LookupReachabilityTarget) + LookupReachabilityAssertions);
        server.Execute("lookup-reachability-down.sql", migrator.GenerateScript(LookupReachabilityTarget, predecessor));
        server.Execute("lookup-reachability-reapply.sql", migrator.GenerateScript(predecessor, LookupReachabilityTarget) + LookupReachabilityAssertions);
    }
#endif

    [Fact]
    public void Gate_entry_uses_its_own_scoped_purchase_order_lookup()
    {
        var root = FindRepositoryRoot();
        var endpoint = File.ReadAllText(Path.Combine(root, "src", "SESS.NexaERP.Api", "Endpoints", "StoresGateEntryEndpoints.cs"));
        var service = File.ReadAllText(Path.Combine(root, "src", "SESS.NexaERP.Infrastructure", "Stores", "EfGateEntryService.Queries.cs"));
        Assert.Contains("/purchase-order-candidates", endpoint);
        Assert.Contains("RequirePagePermission(Page,PagePermissionActions.Create)", endpoint);
        Assert.Contains("RequireReceiptOperatorAsync(ct)", service);
        Assert.Contains("GateEntryPurchaseOrderLineCandidate", service);
        Assert.DoesNotContain("purchase.po", endpoint);
    }

    private const string LookupReachabilityAssertions = """
        DO $assert$ DECLARE actual text; BEGIN
          SELECT string_agg(role."Code"||':'||page."PageKey",',' ORDER BY role."Code",page."PageKey") INTO actual
          FROM advance.role_page_permissions permission
          JOIN advance.roles role ON role."Id"=permission."RoleId"
          JOIN advance.page_definitions page ON page."Id"=permission."PageDefinitionId"
          WHERE permission."CreatedBy"='LookupReachabilityCorrections'
            AND permission."CanView";
          IF actual<>'PRODUCTION_MANAGER:masters.uoms,QC_MANAGER:production.job-orders,TECHNICAL_SUPPORT_MANAGER:masters.uoms,TECHNICAL_SUPPORT_MANAGER:production.job-orders' THEN
            RAISE EXCEPTION 'Lookup grants differ: %',actual;
          END IF;
          IF (SELECT count(*) FROM advance.role_page_permissions WHERE "CreatedBy"='LookupReachabilityCorrections')<>4 THEN
            RAISE EXCEPTION 'Lookup reachability must create exactly four permission rows.';
          END IF;
        END $assert$;
        """;
}