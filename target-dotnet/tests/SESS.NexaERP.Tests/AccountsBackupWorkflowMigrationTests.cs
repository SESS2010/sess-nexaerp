using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    [Fact]
    public void NamedAccountsWorkflowMigrationRoundTripsWithoutChangingExistingDecisionGuardMetadata()
    {
        const string before = "20261017160000_NamedAccountsBackupIdentity";
        const string after = "20261017170000_NamedAccountsWorkflowSubstitution";
        using var db = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);
        var migrator = db.GetService<IMigrator>();
        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("accounts-workflow-before.sql", migrator.GenerateScript("0", before));
        server.Execute("accounts-workflow-snapshot.sql", """
            CREATE TABLE public.accounts_workflow_guard_before AS SELECT to_jsonb(p) data FROM pg_proc p
              WHERE p.oid='advance.purchase_approval_decision_guard()'::regprocedure;
            """);
        var up = migrator.GenerateScript(before, after);
        var down = migrator.GenerateScript(after, before);
        server.Execute("accounts-workflow-up.sql", up);
        server.Execute("accounts-workflow-up-metadata.sql", """
            DO $assert$ BEGIN
              IF EXISTS(SELECT 1 FROM public.accounts_workflow_guard_before b CROSS JOIN pg_proc p
                WHERE p.oid='advance.purchase_approval_decision_guard()'::regprocedure
                  AND (to_jsonb(p)-'prosrc') IS DISTINCT FROM (b.data-'prosrc')) OR
                NOT EXISTS(SELECT 1 FROM pg_proc p
                  WHERE p.oid='advance.accounts_backup_step_actor(uuid,uuid,text,integer,jsonb,uuid,uuid,text,uuid)'::regprocedure
                    AND p.prosecdef AND p.proconfig=ARRAY['search_path=pg_catalog, advance']) OR
                EXISTS(SELECT 1 FROM pg_proc p CROSS JOIN LATERAL aclexplode(coalesce(p.proacl,acldefault('f',p.proowner))) a
                  WHERE p.oid='advance.accounts_backup_step_actor(uuid,uuid,text,integer,jsonb,uuid,uuid,text,uuid)'::regprocedure
                    AND a.grantee=0) THEN
                RAISE EXCEPTION 'Accounts workflow guard metadata or PUBLIC privileges changed.';
              END IF;
              IF advance.accounts_backup_step_actor(gen_random_uuid(),gen_random_uuid(),'unmapped',1,
                '{"steps":[]}'::jsonb,NULL,NULL,'purchase_requisitions',gen_random_uuid()) THEN
                RAISE EXCEPTION 'An unmapped actor must never become an accounts substitute.';
              END IF;
            END $assert$;
            """);
        server.Execute("accounts-workflow-roundtrip-down.sql", down);
        server.Execute("accounts-workflow-restored.sql", """
            DO $assert$ BEGIN
              IF EXISTS(SELECT 1 FROM public.accounts_workflow_guard_before b CROSS JOIN pg_proc p
                WHERE p.oid='advance.purchase_approval_decision_guard()'::regprocedure AND to_jsonb(p) IS DISTINCT FROM b.data) OR
                to_regprocedure('advance.accounts_backup_step_actor(uuid,uuid,text,integer,jsonb,uuid,uuid,text,uuid)') IS NOT NULL THEN
                RAISE EXCEPTION 'Accounts workflow rollback failed to restore the exact existing guard.';
              END IF;
            END $assert$;
            """);
        server.Execute("accounts-workflow-reapply.sql", up);
    }
}
