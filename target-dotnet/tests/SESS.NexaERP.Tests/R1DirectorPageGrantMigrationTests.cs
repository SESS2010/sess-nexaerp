using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void R1_director_grants_are_additive_and_rollback_preserves_existing_permissions(bool repairPending)
    {
        using var model = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);
        var migrator = model.GetService<IMigrator>();
        const string previous = "20260928100000_MachineDeliverySelection";
        const string target = "20260929110000_R1DirectorPageGrants";
        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("director-baseline.sql", migrator.GenerateScript("0", previous));
        using var connection = new NpgsqlConnection(server.ConnectionString);
        connection.Open();
        using (var command = new NpgsqlCommand("""
            SELECT string_agg(r."Code",',' ORDER BY r."Code") FROM advance.role_page_permissions g
            JOIN advance.roles r ON r."Id"=g."RoleId" JOIN advance.page_definitions p ON p."Id"=g."PageDefinitionId"
            WHERE p."PageKey"='tracking.pending' AND g."CanView" AND g."CanViewAuditHistory"
              AND r."Code" IN('STORES_MANAGER','STORES_EXECUTIVE','QC_MANAGER','ACCOUNTS_MANAGER','TECHNICAL_DIRECTOR','MANAGING_DIRECTOR');
            """, connection))
            Console.WriteLine("Pending before migration 140: " + command.ExecuteScalar());
        if (repairPending)
            server.Execute("director-custom-existing.sql", """
                DELETE FROM advance.role_page_permissions g USING advance.roles r
                  WHERE r."Id"=g."RoleId" AND r."Code"='ACCOUNTS_MANAGER' AND g."PageDefinitionId"=md5('tracking.pending')::uuid;
                UPDATE advance.role_page_permissions g SET "CanView"=false,"CanViewAuditHistory"=false,
                    "CanApprove"=true,"UpdatedBy"='existing-custom-grant',"Version"=17
                  FROM advance.roles r WHERE r."Id"=g."RoleId" AND r."Code"='STORES_MANAGER'
                    AND g."PageDefinitionId"=md5('tracking.pending')::uuid;
                """);
        server.Execute("director-snapshot.sql", """
            CREATE TABLE public.director_before AS SELECT "Id",to_jsonb(g) body FROM advance.role_page_permissions g;
            """);
        var up = migrator.GenerateScript(previous, target);
        var down = migrator.GenerateScript(target, previous);
        server.Execute("director-up.sql", up);
        server.Execute("director-check.sql", """
            DO $check$ BEGIN
              IF (SELECT count(*) FROM advance.page_definitions WHERE "IsActive" AND "PageKey" LIKE 'accounts.%')<>5
                OR (SELECT count(*) FROM advance.page_definitions WHERE "IsActive" AND "PageKey" LIKE 'production.%')<>4 THEN
                RAISE EXCEPTION 'Review the approved R1 page catalogue when new pages are introduced.'; END IF;
              IF EXISTS(SELECT 1 FROM advance.roles r CROSS JOIN advance.page_definitions p
                LEFT JOIN advance.role_page_permissions g ON g."RoleId"=r."Id" AND g."PageDefinitionId"=p."Id"
                WHERE p."IsActive" AND (
                  (r."Code"='TECHNICAL_DIRECTOR' AND (p."PageKey" IN('stores.material-issues','production.component-fitments') OR p."PageKey" LIKE 'accounts.%'))
                  OR (r."Code"='MANAGING_DIRECTOR' AND (p."PageKey" LIKE 'production.%' OR p."PageKey" LIKE 'accounts.%'))
                  OR (r."Code" IN('STORES_MANAGER','STORES_EXECUTIVE','QC_MANAGER','ACCOUNTS_MANAGER','TECHNICAL_DIRECTOR','MANAGING_DIRECTOR') AND p."PageKey"='tracking.pending'))
                AND (g."Id" IS NULL OR NOT g."CanView" OR (p."PageKey"='tracking.pending' AND NOT g."CanViewAuditHistory"))) THEN
                RAISE EXCEPTION 'An approved page grant is missing.'; END IF;
              IF EXISTS(SELECT 1 FROM public.director_before b LEFT JOIN advance.role_page_permissions g ON g."Id"=b."Id"
                WHERE g."Id" IS NULL OR (b.body-'CanView'-'CanViewAuditHistory'-'UpdatedAt'-'UpdatedBy'-'Version')
                  IS DISTINCT FROM (to_jsonb(g)-'CanView'-'CanViewAuditHistory'-'UpdatedAt'-'UpdatedBy'-'Version')
                  OR ((b.body->>'CanView')::boolean AND NOT g."CanView")
                  OR ((b.body->>'CanViewAuditHistory')::boolean AND NOT g."CanViewAuditHistory")) THEN
                RAISE EXCEPTION 'An existing permission was removed or another action changed.'; END IF;
              IF EXISTS(SELECT 1 FROM public.director_before b JOIN advance.role_page_permissions g ON g."Id"=b."Id"
                WHERE NOT EXISTS(SELECT 1 FROM advance.r1_director_page_grant_backup j WHERE j.permission_id=g."Id")
                  AND b.body IS DISTINCT FROM to_jsonb(g)) THEN
                RAISE EXCEPTION 'A non-target permission was changed.'; END IF;
              IF EXISTS(SELECT 1 FROM advance.role_page_permissions g
                WHERE NOT EXISTS(SELECT 1 FROM public.director_before b WHERE b."Id"=g."Id")
                  AND (g."CreatedBy"<>'R1DirectorPageGrants' OR EXISTS(SELECT 1 FROM jsonb_each(to_jsonb(g)) e
                    WHERE e.value='true'::jsonb AND e.key NOT IN('CanView','CanViewAuditHistory')))) THEN
                RAISE EXCEPTION 'A new row grants more than the approved read actions.'; END IF;
            END $check$;
            CREATE TABLE public.director_after AS SELECT "Id",to_jsonb(g) body FROM advance.role_page_permissions g;
            """);
        using (var command = new NpgsqlCommand("""
            SELECT string_agg(r."Code"||': '||p."PageKey",E'\n' ORDER BY r."Code",p."PageKey")
              FROM advance.role_page_permissions g JOIN advance.roles r ON r."Id"=g."RoleId"
              JOIN advance.page_definitions p ON p."Id"=g."PageDefinitionId"
              WHERE r."Code" IN('TECHNICAL_DIRECTOR','MANAGING_DIRECTOR') AND p."IsActive" AND (g."CanView" OR g."HasFullControl");
            """, connection))
            Console.WriteLine("Resulting director page grants:\n" + command.ExecuteScalar());
        server.Execute("director-change.sql", """
            UPDATE advance.role_page_permissions SET "CanExport"=true WHERE "Id"=(
              SELECT permission_id FROM advance.r1_director_page_grant_backup ORDER BY permission_id LIMIT 1);
            """);
        server.AssertRejected("director-down-refused.sql", down, "refuses changed or removed permissions");
        server.Execute("director-change-back.sql", """
            UPDATE advance.role_page_permissions g SET "CanExport"=(b.after_row->>'CanExport')::boolean
              FROM advance.r1_director_page_grant_backup b WHERE b.permission_id=g."Id";
            """);
        server.Execute("director-down.sql", down);
        server.Execute("director-roundtrip.sql", """
            DO $check$ BEGIN
              IF EXISTS((SELECT "Id",body FROM public.director_before EXCEPT SELECT "Id",to_jsonb(g) FROM advance.role_page_permissions g)
                UNION ALL (SELECT "Id",to_jsonb(g) FROM advance.role_page_permissions g EXCEPT SELECT "Id",body FROM public.director_before)) THEN
                RAISE EXCEPTION 'Rollback did not restore every original permission exactly.'; END IF;
              IF to_regclass('advance.r1_director_page_grant_backup') IS NOT NULL THEN
                RAISE EXCEPTION 'Rollback journal remains.'; END IF;
            END $check$;
            """);
        server.Execute("director-up-again.sql", up);
        server.Execute("director-reapply-check.sql", """
            DO $check$ BEGIN
              IF EXISTS((SELECT "Id",body-'CreatedAt'-'UpdatedAt' FROM public.director_after EXCEPT
                  SELECT "Id",to_jsonb(g)-'CreatedAt'-'UpdatedAt' FROM advance.role_page_permissions g)
                UNION ALL (SELECT "Id",to_jsonb(g)-'CreatedAt'-'UpdatedAt' FROM advance.role_page_permissions g EXCEPT
                  SELECT "Id",body-'CreatedAt'-'UpdatedAt' FROM public.director_after)) THEN
                RAISE EXCEPTION 'Reapply changed the grant set.'; END IF;
            END $check$;
            """);
    }
}
