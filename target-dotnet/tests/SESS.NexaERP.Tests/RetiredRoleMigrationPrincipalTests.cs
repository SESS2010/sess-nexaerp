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
    public async Task RetiredEmptyRolesDoNotRequireMembershipForOrdinaryMigration75(bool ownerAclAlreadyUsable)
    {
        using var model = new NexaErpDbContext(new DbContextOptionsBuilder<NexaErpDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=no_connect;Username=no_connect",
                options => options.MigrationsHistoryTable("__EFMigrationsHistory", "advance")).Options);
        var migrator = model.GetService<IMigrator>();
        var migrations = model.Database.GetMigrations().ToArray();
        var predecessor = migrations[Array.IndexOf(migrations, OrdinaryOwnerAclTarget) - 1];
        var up = migrator.GenerateScript(predecessor, OrdinaryOwnerAclTarget);
        var down = migrator.GenerateScript(OrdinaryOwnerAclTarget, predecessor);
        using var server = DisposablePostgreSql.Start(FindPostgreSqlBin(), databaseName: "sess_nexa_erp");
        server.Execute("legacy75-schema.sql", migrator.GenerateScript("0", predecessor));
        // Synthetic regression fixture, not evidence of a restored field installation.
        server.Execute("legacy75-retired-state.sql", CompleteRetiredDefectState + """
            ALTER DEFAULT PRIVILEGES FOR ROLE nexa_rev869b_security_owner REVOKE EXECUTE ON FUNCTIONS FROM PUBLIC;
            CREATE ROLE nexa_erp_migration LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS;
            CREATE ROLE nexa_erp_bootstrap LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS;
            CREATE ROLE nexa_erp_runtime LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS;
            GRANT nexa_erp_owner TO nexa_erp_migration WITH INHERIT FALSE, SET TRUE;
            """);
        using var environment = new OrdinaryPrincipalEnvironment(server.ConnectionString, "unused-existing-runtime-password");
        // Exercise both initially revoked and initially usable owner ACLs.
        if (ownerAclAlreadyUsable)
            server.Execute("legacy75-usable-owner-fixture.sql", """
                GRANT USAGE,CREATE ON SCHEMA advance TO nexa_erp_owner;
                GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA advance TO nexa_erp_owner;
                GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA advance TO nexa_erp_owner;
                GRANT ALL PRIVILEGES ON ALL FUNCTIONS IN SCHEMA advance TO nexa_erp_owner;
                """);
        Assert.Equal(0, await DatabasePrincipalCommand.RunAsync(["database-principals", "provision"]));
        Assert.Equal(0, await DatabasePrincipalCommand.RunAsync(["database-principals", "status"]));
        const string actor = """
            SET SESSION AUTHORIZATION nexa_erp_migration;
            SET ROLE nexa_erp_owner;
            DO $actor$ BEGIN
              IF session_user<>'nexa_erp_migration' OR current_user<>'nexa_erp_owner'
                 OR current_setting('is_superuser')<>'off'
                 OR pg_has_role(current_user,'nexa_rev869b_security_owner','MEMBER')
              THEN RAISE EXCEPTION 'Wrong migration actor or unexpected legacy membership'; END IF;
            END $actor$;
            """ + "\n";
        server.Execute("legacy75-owner-fk-before-migration.sql", actor + OwnerForeignKeyExercise);
        var failures = new[]
        {
            ("schema-usage", "REVOKE USAGE ON SCHEMA advance FROM nexa_erp_owner;",
                "SELECT count(*) FROM advance.ordinary_owner_acl_parent;"),
            ("schema-create", "REVOKE CREATE ON SCHEMA advance FROM nexa_erp_owner;",
                "CREATE TABLE advance.owner_acl_should_refuse(id integer);"),
            ("table-select", "REVOKE SELECT ON TABLE advance.ordinary_owner_acl_parent FROM nexa_erp_owner;",
                "SELECT count(*) FROM advance.ordinary_owner_acl_parent;"),
            ("table-references", "REVOKE REFERENCES ON TABLE advance.ordinary_owner_acl_parent FROM nexa_erp_owner;",
                "CREATE TABLE advance.owner_acl_should_refuse(parent_id integer REFERENCES advance.ordinary_owner_acl_parent(id));"),
            ("sequence", "REVOKE ALL PRIVILEGES ON SEQUENCE advance.ordinary_owner_acl_sequence FROM nexa_erp_owner;",
                "SELECT nextval('advance.ordinary_owner_acl_sequence');"),
            ("function", "REVOKE EXECUTE ON FUNCTION advance.ordinary_owner_acl_function() FROM nexa_erp_owner;",
                "SELECT advance.ordinary_owner_acl_function();")
        };
        foreach (var (name, revoke, operation) in failures)
        {
            server.Execute($"legacy75-revoke-{name}.sql", revoke);
            Assert.NotEqual(0, await DatabasePrincipalCommand.RunAsync(["database-principals", "status"]));
            server.AssertRejected($"legacy75-owner-refused-{name}.sql", actor + operation, "permission denied");
            Assert.Equal(0, await DatabasePrincipalCommand.RunAsync(["database-principals", "provision"]));
            Assert.Equal(0, await DatabasePrincipalCommand.RunAsync(["database-principals", "status"]));
            server.Execute($"legacy75-owner-fk-after-{name}.sql", actor + OwnerForeignKeyExercise);
        }
        server.Execute("legacy75-migration-up.sql", actor + up + BasicExplicitAclAssertions + CompleteObjectAclAssertions);
        server.Execute("legacy75-default-acl-retained.sql", """
            DO $retained$ BEGIN
              IF NOT EXISTS(SELECT 1 FROM pg_default_acl WHERE defaclrole='nexa_rev869b_security_owner'::regrole)
              THEN RAISE EXCEPTION 'Migration changed retained default privileges'; END IF;
            END $retained$;
            """);
        server.Execute("legacy75-migration-down.sql", actor + down);
        server.Execute("legacy75-residual-owner.sql",
            "ALTER TABLE advance.ordinary_owner_acl_parent OWNER TO nexa_rev869b_security_owner;");
        server.AssertRejected("legacy75-refuse-unprepared.sql", actor + up, "ownership preparation is required");
        server.Execute("legacy75-refusal-atomic.sql", """
            DO $atomic$ BEGIN
              IF EXISTS(SELECT 1 FROM advance."__EFMigrationsHistory"
                WHERE "MigrationId"='20260911125548_ConvergeOrdinaryOwnerExplicitAcl')
                OR (SELECT relowner FROM pg_class WHERE oid='advance.ordinary_owner_acl_parent'::regclass)
                   <>'nexa_rev869b_security_owner'::regrole
              THEN RAISE EXCEPTION 'Refused migration changed ownership or migration history'; END IF;
            END $atomic$;
            """);
        Assert.Equal(0, await DatabasePrincipalCommand.RunAsync(["database-principals", "provision"]));
        server.Execute("legacy75-prepared-reapply.sql", actor + up + BasicExplicitAclAssertions + CompleteObjectAclAssertions);
        Assert.Equal(0, await DatabasePrincipalCommand.RunAsync(["database-principals", "provision"]));
        Assert.Equal(0, await DatabasePrincipalCommand.RunAsync(["database-principals", "status"]));
    }

    private const string OwnerForeignKeyExercise = """
        BEGIN;
        CREATE TABLE advance.owner_acl_fk_probe(
          parent_id integer NOT NULL REFERENCES advance.ordinary_owner_acl_parent(id));
        INSERT INTO advance.ordinary_owner_acl_parent(id)
          SELECT coalesce(max(id),0)+1 FROM advance.ordinary_owner_acl_parent;
        INSERT INTO advance.owner_acl_fk_probe(parent_id)
          SELECT max(id) FROM advance.ordinary_owner_acl_parent;
        DO $fk$ BEGIN
          BEGIN
            INSERT INTO advance.owner_acl_fk_probe(parent_id) VALUES(-2147483647);
            RAISE EXCEPTION 'Foreign key accepted a missing parent under the owner identity';
          EXCEPTION WHEN foreign_key_violation THEN NULL;
          END;
        END $fk$;
        SELECT count(*) FROM advance.owner_acl_fk_probe;
        SELECT nextval('advance.ordinary_owner_acl_sequence');
        SELECT advance.ordinary_owner_acl_function();
        ROLLBACK;
        """;
}
