using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    private const string MirJournalBefore = "20261003090000_R1RosterSeedReconciliation";
    private const string MirJournalAfter = "20261003113000_MirRollbackJournalProtection";
    private const string MirJournalSnapshotSql = """
        CREATE TABLE public.mir_journal_before AS SELECT md5(jsonb_agg(to_jsonb(b) ORDER BY component)::text) digest
          FROM advance.r1_mir_pending_scope_backup b;
        CREATE TABLE public.mir_journal_functions_before AS
          SELECT p.oid::regprocedure::text signature,replace(pg_get_functiondef(p.oid),E'\r\n',E'\n') definition,
            p.proacl,p.proowner,p.prosecdef,p.proconfig FROM pg_proc p
          WHERE p.oid IN('advance.tracking_history(text,uuid,uuid[],text,text,uuid)'::regprocedure,
            'advance.stores_workload(text,uuid,uuid[],text,text,uuid,bigint,integer)'::regprocedure);
        """;
    private const string MirJournalAssertSql = """
        DO $check$ DECLARE actor text; BEGIN
          IF (SELECT count(*) FROM advance.r1_mir_pending_scope_backup)<>4
            OR (SELECT md5(jsonb_agg(to_jsonb(b) ORDER BY component)::text) FROM advance.r1_mir_pending_scope_backup b)
               IS DISTINCT FROM (SELECT digest FROM public.mir_journal_before)
            OR (SELECT pg_get_userbyid(relowner) FROM pg_class WHERE oid='advance.r1_mir_pending_scope_backup'::regclass)<>'nexa_erp_owner'
            OR EXISTS(SELECT 1 FROM pg_class c
              CROSS JOIN LATERAL aclexplode(coalesce(c.relacl,acldefault('r',c.relowner))) acl
              WHERE c.oid='advance.r1_mir_pending_scope_backup'::regclass AND acl.grantee<>c.relowner) THEN
            RAISE EXCEPTION 'MIR rollback journal lost backup rows or owner-only protection.';
          END IF;
          FOREACH actor IN ARRAY ARRAY['nexa_erp_runtime','nexa_erp_bootstrap'] LOOP
            IF has_table_privilege(actor,'advance.r1_mir_pending_scope_backup','SELECT,INSERT,UPDATE,DELETE,TRUNCATE,REFERENCES,TRIGGER') THEN
              RAISE EXCEPTION 'Non-owner gained MIR rollback journal access.';
            END IF;
          END LOOP;
          IF EXISTS(SELECT 1 FROM public.mir_journal_functions_before original JOIN pg_proc p ON p.oid=original.signature::regprocedure
            WHERE replace(pg_get_functiondef(p.oid),E'\r\n',E'\n') IS DISTINCT FROM original.definition
              OR p.proacl IS DISTINCT FROM original.proacl OR p.proowner<>original.proowner
              OR p.prosecdef<>original.prosecdef OR p.proconfig IS DISTINCT FROM original.proconfig) THEN
            RAISE EXCEPTION 'Journal protection changed an installed MIR read function contract.';
          END IF;
        END $check$;
        """;

    [Fact]
    public void MirRollbackJournalRoundTripRepairsPriorAclAndSurvivesInstallerReconciliation()
    {
        using var model = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);
        var migrator = model.GetService<IMigrator>();
        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("mir-journal-before.sql",migrator.GenerateScript("0",MirJournalBefore));
        server.Execute("mir-journal-before-provision.sql",InstallerPasswordSettings+DatabasePrincipalProvisioningSql.Provision+DatabasePrincipalProvisioningSql.Verify);
        server.Execute("mir-journal-snapshot.sql",MirJournalSnapshotSql);
        // Reproduce an existing installer's broad grant and an unrelated direct reader grant.
        server.Execute("mir-journal-old-acl.sql", """
            CREATE ROLE mir_journal_probe NOLOGIN;
            GRANT SELECT,INSERT,UPDATE ON advance.r1_mir_pending_scope_backup TO nexa_erp_runtime;
            GRANT SELECT ON advance.r1_mir_pending_scope_backup TO PUBLIC,mir_journal_probe;
            DO $check$ BEGIN
              IF NOT has_table_privilege('nexa_erp_runtime','advance.r1_mir_pending_scope_backup','UPDATE') THEN
                RAISE EXCEPTION 'Prior unsafe journal grant was not reproduced.';
              END IF;
            END $check$;
            """);
        var up=migrator.GenerateScript(MirJournalBefore,MirJournalAfter);
        var down=migrator.GenerateScript(MirJournalAfter,MirJournalBefore);
        server.Execute("mir-journal-up.sql",up);
        server.Execute("mir-journal-up-check.sql",MirJournalAssertSql);
        server.AssertRejected("mir-journal-runtime-read-refused.sql",
            "SET SESSION AUTHORIZATION nexa_erp_runtime; SELECT * FROM advance.r1_mir_pending_scope_backup;",
            "permission denied for table r1_mir_pending_scope_backup");
        server.AssertRejected("mir-journal-runtime-tamper-refused.sql",
            "SET SESSION AUTHORIZATION nexa_erp_runtime; UPDATE advance.r1_mir_pending_scope_backup SET before_value='runtime tamper';",
            "permission denied for table r1_mir_pending_scope_backup");
        server.Execute("mir-journal-down.sql",down);
        server.Execute("mir-journal-down-protected.sql",MirJournalAssertSql);
        server.Execute("mir-journal-reapply.sql",up);
        server.Execute("mir-journal-reprovision.sql",InstallerPasswordSettings+DatabasePrincipalProvisioningSql.Provision+DatabasePrincipalProvisioningSql.Verify);
        server.Execute("mir-journal-reprovision-check.sql",MirJournalAssertSql);
    }

    [Fact]
    public void MirRollbackJournalVerifierRefusesUnexpectedGrantsAndProvisioningRepairsThem()
    {
        using var model = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);
        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin());
        server.Execute("mir-journal-fresh.sql",model.GetService<IMigrator>().GenerateScript("0",MirJournalAfter));
        server.Execute("mir-journal-fresh-provision.sql",InstallerPasswordSettings+DatabasePrincipalProvisioningSql.Provision+DatabasePrincipalProvisioningSql.Verify);
        server.Execute("mir-journal-fresh-snapshot.sql",MirJournalSnapshotSql);
        server.Execute("mir-journal-grant-probe.sql", """
            CREATE ROLE mir_journal_probe NOLOGIN;
            GRANT SELECT,UPDATE ON advance.r1_mir_pending_scope_backup TO mir_journal_probe;
            """);
        server.AssertRejected("mir-journal-verify-refused.sql",DatabasePrincipalProvisioningSql.Verify,
            "R1 rollback journals must grant no privilege outside their owner");
        server.Execute("mir-journal-repair.sql",InstallerPasswordSettings+DatabasePrincipalProvisioningSql.Provision+DatabasePrincipalProvisioningSql.Verify);
        server.Execute("mir-journal-repair-check.sql",MirJournalAssertSql);
    }
}
