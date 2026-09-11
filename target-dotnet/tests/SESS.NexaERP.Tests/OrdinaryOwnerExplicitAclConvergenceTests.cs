using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SESS.NexaERP.Infrastructure.Persistence;

namespace SESS.NexaERP.Tests;

public sealed partial class AdvanceMigrationSqlSyntaxTests
{
    private const string OrdinaryOwnerAclTarget = "20260911125548_ConvergeOrdinaryOwnerExplicitAcl";

    [Fact]
    public void Ordinary_owner_acl_convergence_handles_absent_complete_and_partial_retirement_states()
    {
        using var model = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect").Options);
        var migrator = model.GetService<IMigrator>();
        var migrations = model.Database.GetMigrations().ToArray();
        var index = Array.IndexOf(migrations, OrdinaryOwnerAclTarget);
        Assert.True(index > 0);
        var predecessor = migrations[index - 1];
        var up = migrator.GenerateScript(predecessor, OrdinaryOwnerAclTarget);
        var down = migrator.GenerateScript(OrdinaryOwnerAclTarget, predecessor);

        using (var absent = DisposablePostgreSql.Start(FindPostgreSqlBin()))
        {
            absent.Execute("ordinary-owner-acl-absent-pre.sql", migrator.GenerateScript("0", predecessor));
            absent.Execute("ordinary-owner-acl-absent-up.sql", up);
            absent.Execute("ordinary-owner-acl-absent-down.sql", down);
            absent.Execute("ordinary-owner-acl-absent-reapply.sql", up);
        }

        using (var provisionedWithoutRev = DisposablePostgreSql.Start(FindPostgreSqlBin()))
        {
            provisionedWithoutRev.Execute("ordinary-owner-acl-no-rev-pre.sql", migrator.GenerateScript("0", predecessor));
            provisionedWithoutRev.Execute("ordinary-owner-acl-no-rev-owner.sql",
                "CREATE ROLE nexa_erp_owner NOLOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS;");
            provisionedWithoutRev.Execute("ordinary-owner-acl-no-rev-up.sql", up + BasicExplicitAclAssertions);
            provisionedWithoutRev.Execute("ordinary-owner-acl-no-rev-down.sql", down + BasicExplicitAclAssertions);
            provisionedWithoutRev.Execute("ordinary-owner-acl-no-rev-reapply.sql", up + BasicExplicitAclAssertions);
        }

        using (var partial = DisposablePostgreSql.Start(FindPostgreSqlBin()))
        {
            partial.Execute("ordinary-owner-acl-partial-pre.sql", migrator.GenerateScript("0", predecessor));
            partial.Execute("ordinary-owner-acl-partial-role.sql", "CREATE ROLE nexa_rev869b_security_owner NOLOGIN;");
            partial.AssertRejected("ordinary-owner-acl-partial-up.sql", up, "partial retired state");
        }

        using var complete = DisposablePostgreSql.Start(FindPostgreSqlBin());
        complete.Execute("ordinary-owner-acl-complete-pre.sql", migrator.GenerateScript("0", predecessor));
        complete.Execute("ordinary-owner-acl-complete-defect.sql", CompleteRetiredDefectState);
        complete.Execute("ordinary-owner-acl-complete-up.sql", up + BasicExplicitAclAssertions + CompleteObjectAclAssertions + OwnerAccessAndForeignKeyWitness);
        complete.Execute("ordinary-owner-acl-complete-down.sql", down + BasicExplicitAclAssertions + CompleteObjectAclAssertions + OwnerAccessAndForeignKeyWitness);
        complete.Execute("ordinary-owner-acl-complete-reapply.sql", up + BasicExplicitAclAssertions + CompleteObjectAclAssertions + OwnerAccessAndForeignKeyWitness);
    }

    private const string CompleteRetiredDefectState = """
        CREATE ROLE nexa_erp_owner NOLOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS;
        CREATE ROLE nexa_rev869b_security_owner NOLOGIN;
        CREATE ROLE nexa_rev869b_lifecycle_administrator NOLOGIN;
        CREATE ROLE nexa_rev869b_app_runtime NOLOGIN;
        CREATE ROLE nexa_rev869b_command_audit NOLOGIN;
        CREATE ROLE nexa_rev869b_management_writer NOLOGIN;
        CREATE ROLE nexa_rev869b_purge_worker NOLOGIN;
        CREATE ROLE nexa_rev869b_purge_audit NOLOGIN;
        CREATE ROLE nexa_rev869b_export_service NOLOGIN;
        CREATE ROLE nexa_rev869b_target_verifier NOLOGIN;
        UPDATE advance.rev869b_retirement_state SET "WasInstalled"=true WHERE "Id";

        CREATE TABLE advance.ordinary_owner_acl_parent(id integer PRIMARY KEY);
        CREATE TABLE advance.ordinary_owner_acl_child(id integer PRIMARY KEY,parent_id integer NOT NULL
          REFERENCES advance.ordinary_owner_acl_parent(id));
        CREATE SEQUENCE advance.ordinary_owner_acl_sequence;
        CREATE FUNCTION advance.ordinary_owner_acl_function() RETURNS integer
          LANGUAGE sql IMMUTABLE AS 'SELECT 1';
        ALTER SCHEMA advance OWNER TO nexa_rev869b_security_owner;
        ALTER TABLE advance.ordinary_owner_acl_parent OWNER TO nexa_rev869b_security_owner;
        ALTER TABLE advance.ordinary_owner_acl_child OWNER TO nexa_rev869b_security_owner;
        ALTER SEQUENCE advance.ordinary_owner_acl_sequence OWNER TO nexa_rev869b_security_owner;
        ALTER FUNCTION advance.ordinary_owner_acl_function() OWNER TO nexa_rev869b_security_owner;

        REVOKE USAGE,CREATE ON SCHEMA advance FROM nexa_rev869b_security_owner;
        REVOKE ALL PRIVILEGES ON TABLE advance.ordinary_owner_acl_parent,advance.ordinary_owner_acl_child
          FROM nexa_rev869b_security_owner;
        REVOKE ALL PRIVILEGES ON SEQUENCE advance.ordinary_owner_acl_sequence FROM nexa_rev869b_security_owner;
        REVOKE ALL PRIVILEGES ON FUNCTION advance.ordinary_owner_acl_function() FROM nexa_rev869b_security_owner;
        REASSIGN OWNED BY nexa_rev869b_security_owner TO nexa_erp_owner;
        """;

    private const string BasicExplicitAclAssertions = """
        DO $assert$
        DECLARE owner_oid oid := (SELECT oid FROM pg_roles WHERE rolname='nexa_erp_owner');
        BEGIN
          IF NOT EXISTS (SELECT 1 FROM pg_namespace n CROSS JOIN LATERAL aclexplode(n.nspacl) acl
                         WHERE n.nspname='advance' AND acl.grantee=owner_oid AND acl.privilege_type='USAGE') THEN
            RAISE EXCEPTION 'nexa_erp_owner lacks explicit schema USAGE.';
          END IF;          IF (SELECT count(DISTINCT acl.privilege_type)
                FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace
                CROSS JOIN LATERAL aclexplode(c.relacl) acl
               WHERE n.nspname='advance' AND c.relname='employees'
                 AND acl.grantee=owner_oid
                 AND acl.privilege_type=ANY(ARRAY['SELECT','INSERT','UPDATE','DELETE','TRUNCATE','REFERENCES','TRIGGER']))<>7 THEN
            RAISE EXCEPTION 'nexa_erp_owner lacks explicit ALL table privileges.';
          END IF;
        END $assert$;
        """;

    private const string CompleteObjectAclAssertions = """
        DO $assert$
        DECLARE owner_oid oid := (SELECT oid FROM pg_roles WHERE rolname='nexa_erp_owner');
        BEGIN
          IF (SELECT count(DISTINCT acl.privilege_type)
                FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace
                CROSS JOIN LATERAL aclexplode(c.relacl) acl
               WHERE n.nspname='advance' AND c.relname='ordinary_owner_acl_sequence'
                 AND acl.grantee=owner_oid
                 AND acl.privilege_type=ANY(ARRAY['USAGE','SELECT','UPDATE']))<>3 THEN
            RAISE EXCEPTION 'nexa_erp_owner lacks explicit ALL sequence privileges.';
          END IF;
          IF NOT EXISTS (SELECT 1 FROM pg_proc p JOIN pg_namespace n ON n.oid=p.pronamespace
                         CROSS JOIN LATERAL aclexplode(p.proacl) acl
                         WHERE n.nspname='advance' AND p.proname='ordinary_owner_acl_function'
                           AND acl.grantee=owner_oid AND acl.privilege_type='EXECUTE') THEN
            RAISE EXCEPTION 'nexa_erp_owner lacks explicit ALL function privileges.';
          END IF;
        END $assert$;
        """;
    private const string OwnerAccessAndForeignKeyWitness = """
        SET ROLE nexa_erp_owner;
        SELECT count(*) FROM advance.ordinary_owner_acl_parent;
        INSERT INTO advance.ordinary_owner_acl_parent(id)
          SELECT COALESCE(max(id),0)+1 FROM advance.ordinary_owner_acl_parent;
        INSERT INTO advance.ordinary_owner_acl_child(id,parent_id)
          SELECT COALESCE((SELECT max(id) FROM advance.ordinary_owner_acl_child),0)+1,max(id)
            FROM advance.ordinary_owner_acl_parent;
        SELECT nextval('advance.ordinary_owner_acl_sequence');
        SELECT advance.ordinary_owner_acl_function();
        RESET ROLE;
        """;
}