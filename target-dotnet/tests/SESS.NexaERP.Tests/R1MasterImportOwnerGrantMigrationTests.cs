using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    [Fact]
    public async Task R1_master_import_owner_grants_are_additive_export_is_separate_and_rollback_refuses_edits()
    {
        using var model = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);
        var migrator = model.GetService<IMigrator>();
        const string previous = "20260929181000_R1PurchaseProductionGrants";
        const string target = "20261002090000_R1MasterImportOwnerGrants";
        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("import-owner-baseline.sql", migrator.GenerateScript("0", previous));
        await using var db = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql(server.ConnectionString).Options);
        const string snapshot = "SELECT jsonb_agg(to_jsonb(g) ORDER BY g.\"Id\")::text AS \"Value\" FROM advance.role_page_permissions g";
        var before = await db.Database.SqlQueryRaw<string>(snapshot).SingleAsync();
        server.Execute("import-owner-before.sql", "CREATE TABLE public.import_owner_before AS SELECT \"Id\",to_jsonb(g) body FROM advance.role_page_permissions g;");
        var up = migrator.GenerateScript(previous, target);
        var down = migrator.GenerateScript(target, previous);
        server.Execute("import-owner-up.sql", up);
        foreach (var page in new[] { "masters.vendors", "masters.items", "masters.uoms", "masters.manufacturers" })
        foreach (var role in new[] { page == "masters.vendors" ? "PURCHASE_MANAGER" : "STORES_MANAGER", "TECHNICAL_DIRECTOR", "MANAGING_DIRECTOR" })
        {
            var grant = await db.RolePagePermissions.AsNoTracking().SingleAsync(x => x.Role!.Code == role && x.PageDefinition!.PageKey == page);
            Assert.True(grant.CanView && grant.CanCreate && grant.CanUpdate && grant.CanDownload, role + " " + page);
        }
        server.Execute("import-owner-preserves.sql", """
            DO $check$ BEGIN
              IF EXISTS(SELECT 1 FROM public.import_owner_before b JOIN advance.role_page_permissions g ON g."Id"=b."Id"
                LEFT JOIN advance.r1_master_import_owner_backup j ON j.permission_id=g."Id"
                WHERE CASE WHEN j.permission_id IS NULL THEN b.body IS DISTINCT FROM to_jsonb(g)
                  ELSE (b.body-'CanView'-'CanCreate'-'CanUpdate'-'CanDownload'-'UpdatedAt'-'UpdatedBy'-'Version') IS DISTINCT FROM
                    (to_jsonb(g)-'CanView'-'CanCreate'-'CanUpdate'-'CanDownload'-'UpdatedAt'-'UpdatedBy'-'Version') END)
                OR EXISTS(SELECT 1 FROM public.import_owner_before b LEFT JOIN advance.role_page_permissions g ON g."Id"=b."Id" WHERE g."Id" IS NULL)
              THEN RAISE EXCEPTION 'Export or unrelated permission changed.'; END IF;
              IF EXISTS(SELECT 1 FROM advance.r1_master_import_owner_backup b
                JOIN advance.role_page_permissions g ON g."Id"=b.permission_id
                WHERE b.before_row IS NULL AND EXISTS(SELECT 1 FROM jsonb_each(to_jsonb(g)) e
                  WHERE e.value='true'::jsonb AND e.key NOT IN('CanView','CanCreate','CanUpdate','CanDownload')))
              THEN RAISE EXCEPTION 'New grant contains unapproved actions.'; END IF;
            END $check$;
            """);
        const string edit = """
            UPDATE advance.role_page_permissions g SET "CanExport"=NOT g."CanExport"
            FROM advance.roles r,advance.page_definitions p WHERE g."RoleId"=r."Id" AND g."PageDefinitionId"=p."Id"
              AND r."Code"='PURCHASE_MANAGER' AND p."PageKey"='masters.vendors';
            """;
        server.Execute("import-owner-edit.sql", edit);
        server.AssertRejected("import-owner-refused-down.sql", down, "refuses changed or removed permissions");
        server.Execute("import-owner-restore-edit.sql", edit);
        server.Execute("import-owner-down.sql", down);
        Assert.Equal(before, await db.Database.SqlQueryRaw<string>(snapshot).SingleAsync());
        server.Execute("import-owner-reapply.sql", up);
        Assert.Contains(target, await db.Database.GetAppliedMigrationsAsync());
    }
}
