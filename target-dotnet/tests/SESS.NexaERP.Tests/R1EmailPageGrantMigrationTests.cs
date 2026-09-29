using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void R1_email_page_grants_preserve_permissions_and_refuse_changed_rollback(bool repairExisting)
    {
        using var model = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);
        var migrator = model.GetService<IMigrator>();
        const string previous = "20260929110000_R1DirectorPageGrants";
        const string target = "20260929123000_R1EmailPageGrants";
        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("email-grants-baseline.sql", migrator.GenerateScript("0", previous));
        if (repairExisting)
            server.Execute("email-grants-existing.sql", """
                UPDATE advance.role_page_permissions g SET "CanView"=false,"CanUpdate"=false,"CanApprove"=true,
                  "UpdatedBy"='existing-custom-grant',"Version"=17
                FROM advance.roles r WHERE r."Id"=g."RoleId" AND r."Code"='TECHNICAL_DIRECTOR'
                  AND g."PageDefinitionId"=md5('admin.email')::uuid;
                DELETE FROM advance.role_page_permissions g USING advance.roles r
                WHERE r."Id"=g."RoleId" AND r."Code"='IT_MANAGER' AND g."PageDefinitionId"=md5('admin.email')::uuid;
                """);
        server.Execute("email-grants-snapshot.sql", """
            CREATE TABLE public.email_grants_before AS SELECT "Id",to_jsonb(g) body FROM advance.role_page_permissions g;
            CREATE TABLE public.email_page_before AS SELECT to_jsonb(p) body FROM advance.page_definitions p;
            """);
        var up = migrator.GenerateScript(previous, target);
        var down = migrator.GenerateScript(target, previous);
        server.Execute("email-grants-up.sql", up);
        server.Execute("email-grants-check.sql", """
            DO $check$ BEGIN
              IF (SELECT string_agg(r."Code"||':'||g."CanView"||':'||g."CanUpdate",',' ORDER BY r."Code")
                FROM advance.role_page_permissions g JOIN advance.roles r ON r."Id"=g."RoleId"
                WHERE g."PageDefinitionId"=md5('admin.email')::uuid)
                IS DISTINCT FROM 'IT_MANAGER:true:false,MANAGING_DIRECTOR:true:true,TECHNICAL_DIRECTOR:true:true' THEN
                RAISE EXCEPTION 'The exact email View/Update grant set is wrong.'; END IF;
              IF NOT EXISTS(SELECT 1 FROM advance.page_definitions WHERE "PageKey"='admin.email' AND "Route"='/admin/email' AND "IsActive") THEN
                RAISE EXCEPTION 'The existing email page definition must remain active.'; END IF;
              IF EXISTS((SELECT body FROM public.email_page_before EXCEPT SELECT to_jsonb(p) FROM advance.page_definitions p)
                UNION ALL (SELECT to_jsonb(p) FROM advance.page_definitions p EXCEPT SELECT body FROM public.email_page_before)) THEN
                RAISE EXCEPTION 'Page definitions were changed.'; END IF;
              IF EXISTS(SELECT 1 FROM public.email_grants_before b LEFT JOIN advance.role_page_permissions g ON g."Id"=b."Id"
                WHERE g."Id" IS NULL OR (b.body-'CanView'-'CanUpdate'-'UpdatedAt'-'UpdatedBy'-'Version')
                  IS DISTINCT FROM (to_jsonb(g)-'CanView'-'CanUpdate'-'UpdatedAt'-'UpdatedBy'-'Version')
                  OR ((b.body->>'CanView')::boolean AND NOT g."CanView")
                  OR ((b.body->>'CanUpdate')::boolean AND NOT g."CanUpdate")) THEN
                RAISE EXCEPTION 'An existing permission was removed or another action changed.'; END IF;
              IF EXISTS(SELECT 1 FROM public.email_grants_before b JOIN advance.role_page_permissions g ON g."Id"=b."Id"
                WHERE NOT EXISTS(SELECT 1 FROM advance.r1_email_page_grant_backup j WHERE j.permission_id=g."Id")
                  AND b.body IS DISTINCT FROM to_jsonb(g)) THEN
                RAISE EXCEPTION 'An unrelated row was changed.'; END IF;
              IF EXISTS(SELECT 1 FROM advance.role_page_permissions g
                WHERE NOT EXISTS(SELECT 1 FROM public.email_grants_before b WHERE b."Id"=g."Id")
                  AND (g."PageDefinitionId"<>md5('admin.email')::uuid OR g."CreatedBy"<>'R1EmailPageGrants'
                    OR EXISTS(SELECT 1 FROM jsonb_each(to_jsonb(g)) e WHERE e.value='true'::jsonb AND e.key NOT IN('CanView','CanUpdate')))) THEN
                RAISE EXCEPTION 'New rows contain unapproved actions.'; END IF;
            END $check$;
            CREATE TABLE public.email_grants_after AS SELECT "Id",to_jsonb(g) body FROM advance.role_page_permissions g;
            """);
        server.Execute("email-grants-change.sql", """
            UPDATE advance.role_page_permissions g SET "CanExport"=true FROM advance.roles r
              WHERE r."Id"=g."RoleId" AND r."Code"='MANAGING_DIRECTOR' AND g."PageDefinitionId"=md5('admin.email')::uuid;
            """);
        server.AssertRejected("email-grants-down-refused.sql", down, "refuses changed or removed permissions");
        server.Execute("email-grants-restore.sql", """
            UPDATE advance.role_page_permissions g SET "CanExport"=(b.after_row->>'CanExport')::boolean
              FROM advance.r1_email_page_grant_backup b WHERE b.permission_id=g."Id";
            """);
        server.Execute("email-grants-down.sql", down);
        server.Execute("email-grants-roundtrip.sql", """
            DO $check$ BEGIN
              IF EXISTS((SELECT "Id",body FROM public.email_grants_before EXCEPT SELECT "Id",to_jsonb(g) FROM advance.role_page_permissions g)
                UNION ALL (SELECT "Id",to_jsonb(g) FROM advance.role_page_permissions g EXCEPT SELECT "Id",body FROM public.email_grants_before)) THEN
                RAISE EXCEPTION 'Rollback did not restore all existing grants exactly.'; END IF;
              IF to_regclass('advance.r1_email_page_grant_backup') IS NOT NULL THEN RAISE EXCEPTION 'Rollback journal remains.'; END IF;
            END $check$;
            """);
        server.Execute("email-grants-up-again.sql", up);
        server.Execute("email-grants-reapply.sql", """
            DO $check$ BEGIN
              IF EXISTS((SELECT "Id",body-'CreatedAt'-'UpdatedAt' FROM public.email_grants_after EXCEPT
                  SELECT "Id",to_jsonb(g)-'CreatedAt'-'UpdatedAt' FROM advance.role_page_permissions g)
                UNION ALL (SELECT "Id",to_jsonb(g)-'CreatedAt'-'UpdatedAt' FROM advance.role_page_permissions g EXCEPT
                  SELECT "Id",body-'CreatedAt'-'UpdatedAt' FROM public.email_grants_after)) THEN
                RAISE EXCEPTION 'Reapply changed the grant set.'; END IF;
            END $check$;
            """);
    }
}
