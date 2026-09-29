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
    public void R1_remaining_director_views_preserve_permissions_and_refuse_changed_rollback(bool repairExisting)
    {
        using var model = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);
        var migrator = model.GetService<IMigrator>();
        const string previous = "20260929123000_R1EmailPageGrants";
        const string target = "20260929130000_R1RemainingDirectorViews";
        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("remaining-director-baseline.sql", migrator.GenerateScript("0", previous));
        if (repairExisting)
            server.Execute("remaining-director-existing.sql", """
                INSERT INTO advance.role_page_permissions
                SELECT (jsonb_populate_record(NULL::advance.role_page_permissions, to_jsonb(g) || jsonb_build_object(
                  'Id',md5('existing-director-test')::uuid,'PageDefinitionId',md5('production.fat-readiness')::uuid,
                  'CanView',false,'CanExport',true,'UpdatedBy','existing-custom-grant','Version',17))).*
                FROM advance.role_page_permissions g JOIN advance.roles r ON r."Id"=g."RoleId"
                WHERE r."Code"='TECHNICAL_DIRECTOR' AND g."PageDefinitionId"=md5('production.production-bom')::uuid;
                """);
        server.Execute("remaining-director-snapshot.sql", """
            CREATE TABLE public.remaining_director_before AS SELECT "Id",to_jsonb(g) body FROM advance.role_page_permissions g;
            CREATE TABLE public.remaining_pages_before AS SELECT to_jsonb(p) body FROM advance.page_definitions p;
            """);
        var up = migrator.GenerateScript(previous, target);
        var down = migrator.GenerateScript(target, previous);
        server.Execute("remaining-director-up.sql", up);
        server.Execute("remaining-director-check.sql", """
            DO $check$ BEGIN
              IF (SELECT string_agg(r."Code"||':'||p."PageKey"||':'||g."CanView",',' ORDER BY r."Code",p."PageKey")
                FROM advance.r1_remaining_director_backup j
                JOIN advance.role_page_permissions g ON g."Id"=j.permission_id
                JOIN advance.roles r ON r."Id"=g."RoleId"
                JOIN advance.page_definitions p ON p."Id"=g."PageDefinitionId")
                IS DISTINCT FROM 'MANAGING_DIRECTOR:stores.machine-deliveries:true,MANAGING_DIRECTOR:stores.material-issues:true,TECHNICAL_DIRECTOR:production.fat-readiness:true' THEN
                RAISE EXCEPTION 'The exact three approved director View grants are wrong.'; END IF;
              IF EXISTS((SELECT body FROM public.remaining_pages_before EXCEPT SELECT to_jsonb(p) FROM advance.page_definitions p)
                UNION ALL (SELECT to_jsonb(p) FROM advance.page_definitions p EXCEPT SELECT body FROM public.remaining_pages_before)) THEN
                RAISE EXCEPTION 'Page definitions were changed.'; END IF;
              IF EXISTS(SELECT 1 FROM public.remaining_director_before b LEFT JOIN advance.role_page_permissions g ON g."Id"=b."Id"
                WHERE g."Id" IS NULL OR (b.body-'CanView'-'UpdatedAt'-'UpdatedBy'-'Version')
                  IS DISTINCT FROM (to_jsonb(g)-'CanView'-'UpdatedAt'-'UpdatedBy'-'Version')
                  OR ((b.body->>'CanView')::boolean AND NOT g."CanView")
                  ) THEN
                RAISE EXCEPTION 'An existing permission was removed or another action changed.'; END IF;
              IF EXISTS(SELECT 1 FROM public.remaining_director_before b JOIN advance.role_page_permissions g ON g."Id"=b."Id"
                WHERE NOT EXISTS(SELECT 1 FROM advance.r1_remaining_director_backup j WHERE j.permission_id=g."Id")
                  AND b.body IS DISTINCT FROM to_jsonb(g)) THEN
                RAISE EXCEPTION 'An unrelated row was changed.'; END IF;
              IF EXISTS(SELECT 1 FROM advance.role_page_permissions g
                WHERE NOT EXISTS(SELECT 1 FROM public.remaining_director_before b WHERE b."Id"=g."Id")
                  AND (g."CreatedBy"<>'R1RemainingDirectorViews'
                    OR EXISTS(SELECT 1 FROM jsonb_each(to_jsonb(g)) e WHERE e.value='true'::jsonb AND e.key NOT IN('CanView')))) THEN
                RAISE EXCEPTION 'New rows contain unapproved actions.'; END IF;
            END $check$;
            CREATE TABLE public.remaining_director_after AS SELECT "Id",to_jsonb(g) body FROM advance.role_page_permissions g;
            """);
        server.Execute("remaining-director-change.sql", """
            UPDATE advance.role_page_permissions g SET "CanExport"=true FROM advance.roles r
              WHERE r."Id"=g."RoleId" AND r."Code"='MANAGING_DIRECTOR' AND g."PageDefinitionId"=md5('stores.machine-deliveries')::uuid;
            """);
        server.AssertRejected("remaining-director-down-refused.sql", down, "refuses changed or removed permissions");
        server.Execute("remaining-director-restore.sql", """
            UPDATE advance.role_page_permissions g SET "CanExport"=(b.after_row->>'CanExport')::boolean
              FROM advance.r1_remaining_director_backup b WHERE b.permission_id=g."Id";
            """);
        server.Execute("remaining-director-down.sql", down);
        server.Execute("remaining-director-roundtrip.sql", """
            DO $check$ BEGIN
              IF EXISTS((SELECT "Id",body FROM public.remaining_director_before EXCEPT SELECT "Id",to_jsonb(g) FROM advance.role_page_permissions g)
                UNION ALL (SELECT "Id",to_jsonb(g) FROM advance.role_page_permissions g EXCEPT SELECT "Id",body FROM public.remaining_director_before)) THEN
                RAISE EXCEPTION 'Rollback did not restore all existing grants exactly.'; END IF;
              IF to_regclass('advance.r1_remaining_director_backup') IS NOT NULL THEN RAISE EXCEPTION 'Rollback journal remains.'; END IF;
            END $check$;
            """);
        server.Execute("remaining-director-up-again.sql", up);
        server.Execute("remaining-director-reapply.sql", """
            DO $check$ BEGIN
              IF EXISTS((SELECT "Id",body-'CreatedAt'-'UpdatedAt' FROM public.remaining_director_after EXCEPT
                  SELECT "Id",to_jsonb(g)-'CreatedAt'-'UpdatedAt' FROM advance.role_page_permissions g)
                UNION ALL (SELECT "Id",to_jsonb(g)-'CreatedAt'-'UpdatedAt' FROM advance.role_page_permissions g EXCEPT
                  SELECT "Id",body-'CreatedAt'-'UpdatedAt' FROM public.remaining_director_after)) THEN
                RAISE EXCEPTION 'Reapply changed the grant set.'; END IF;
            END $check$;
            """);
    }
}
