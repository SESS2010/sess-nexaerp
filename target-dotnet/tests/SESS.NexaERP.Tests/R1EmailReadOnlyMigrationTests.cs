using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    [Fact]
    public async Task R1_email_log_is_director_view_only_and_rollback_preserves_prior_grants()
    {
        using var model = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);
        var migrator = model.GetService<IMigrator>();
        const string previous = "20261002090000_R1MasterImportOwnerGrants";
        const string target = "20261002140000_R1EmailLogReadOnly";
        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("email-read-only-baseline.sql", migrator.GenerateScript("0", previous));
        server.Execute("email-read-only-site-fixture.sql", """
            INSERT INTO advance.role_page_permissions
            SELECT (jsonb_populate_record(NULL::advance.role_page_permissions,to_jsonb(g)||jsonb_build_object(
              'Id',md5('email-site-permission')::uuid,'RoleId',(SELECT "Id" FROM advance.roles WHERE "Code"='PURCHASE_MANAGER'),
              'CanView',true,'CanUpdate',true,'CanApprove',true,'HasFullControl',true,'CreatedBy','SYNTHETIC_EMAIL_SITE'))).*
            FROM advance.role_page_permissions g JOIN advance.roles r ON r."Id"=g."RoleId"
            WHERE r."Code"='TECHNICAL_DIRECTOR' AND g."PageDefinitionId"=md5('admin.email')::uuid;
            DELETE FROM advance.role_page_permissions g USING advance.roles r
              WHERE g."RoleId"=r."Id" AND r."Code"='MANAGING_DIRECTOR' AND g."PageDefinitionId"=md5('admin.email')::uuid;
            CREATE TABLE public.email_read_only_before AS SELECT "Id",to_jsonb(g) body FROM advance.role_page_permissions g;
            """);
        await using var db = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>().UseNpgsql(server.ConnectionString).Options);
        const string snapshot = "SELECT jsonb_agg(to_jsonb(g) ORDER BY g.\"Id\")::text AS \"Value\" FROM advance.role_page_permissions g";
        var before = await db.Database.SqlQueryRaw<string>(snapshot).SingleAsync();
        var up = migrator.GenerateScript(previous, target);
        var down = migrator.GenerateScript(target, previous);
        server.Execute("email-read-only-up.sql", up);
        server.Execute("email-read-only-exact-grants.sql", """
            DO $check$ BEGIN
              IF (SELECT string_agg(r."Code"||':'||g."CanView"||':'||g."CanUpdate",',' ORDER BY r."Code")
                FROM advance.role_page_permissions g JOIN advance.roles r ON r."Id"=g."RoleId"
                WHERE g."PageDefinitionId"=md5('admin.email')::uuid) <>
                'IT_MANAGER:false:false,MANAGING_DIRECTOR:true:false,PURCHASE_MANAGER:false:false,TECHNICAL_DIRECTOR:true:false' THEN
                RAISE EXCEPTION 'R1 email view must belong exactly to TD and MD.';
              END IF;
              IF EXISTS(SELECT 1 FROM advance.role_page_permissions g CROSS JOIN LATERAL jsonb_each(to_jsonb(g)) e
                WHERE g."PageDefinitionId"=md5('admin.email')::uuid AND e.value='true'::jsonb AND e.key<>'CanView') THEN
                RAISE EXCEPTION 'R1 email page must have no action or full-control grant.';
              END IF;
              IF EXISTS(SELECT 1 FROM public.email_read_only_before b JOIN advance.role_page_permissions g ON g."Id"=b."Id"
                WHERE g."PageDefinitionId"<>md5('admin.email')::uuid AND b.body IS DISTINCT FROM to_jsonb(g)) THEN
                RAISE EXCEPTION 'An unrelated page grant changed.';
              END IF;
            END $check$;
            """);
        const string drift = """
            UPDATE advance.role_page_permissions g SET "CanUpdate"=NOT g."CanUpdate" FROM advance.roles r
            WHERE g."RoleId"=r."Id" AND r."Code"='TECHNICAL_DIRECTOR' AND g."PageDefinitionId"=md5('admin.email')::uuid;
            """;
        server.Execute("email-read-only-drift.sql", drift);
        server.AssertRejected("email-read-only-refuse.sql", down, "refuses changed or added permissions");
        server.Execute("email-read-only-restore.sql", drift);
        server.Execute("email-read-only-down.sql", down);
        Assert.Equal(before, await db.Database.SqlQueryRaw<string>(snapshot).SingleAsync());
        server.Execute("email-read-only-reapply.sql", up);
        Assert.Contains(target, await db.Database.GetAppliedMigrationsAsync());
    }
}
