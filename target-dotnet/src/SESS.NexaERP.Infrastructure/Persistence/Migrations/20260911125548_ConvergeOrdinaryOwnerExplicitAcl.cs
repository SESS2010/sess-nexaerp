using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SESS.NexaERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ConvergeOrdinaryOwnerExplicitAcl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            PostgreSqlClusterGuard.Require(migrationBuilder);
            migrationBuilder.Sql(Converge);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PostgreSqlClusterGuard.Require(migrationBuilder);
            // This security correction is monotonic: rollback retains the repaired owner ACL.
            migrationBuilder.Sql(Converge);
        }

        private const string Converge = """
            DO $converge$
            DECLARE
              was_installed boolean;
              role_count integer;
              role_name text;
              target_roles constant text[] := ARRAY[
                'nexa_rev869b_security_owner','nexa_rev869b_lifecycle_administrator',
                'nexa_rev869b_app_runtime','nexa_rev869b_command_audit',
                'nexa_rev869b_management_writer','nexa_rev869b_purge_worker',
                'nexa_rev869b_purge_audit','nexa_rev869b_export_service',
                'nexa_rev869b_target_verifier'];
            BEGIN
              IF to_regclass('advance.rev869b_retirement_state') IS NULL THEN
                RAISE EXCEPTION USING ERRCODE='55000',
                  MESSAGE='Refusing ordinary-owner ACL convergence: REV869B retirement state is missing.';
              END IF;
              SELECT "WasInstalled" INTO STRICT was_installed
                FROM advance.rev869b_retirement_state WHERE "Id";
              SELECT count(*) INTO role_count FROM pg_roles WHERE rolname=ANY(target_roles);
              IF (NOT was_installed AND role_count<>0)
                 OR (was_installed AND role_count<>cardinality(target_roles)) THEN
                RAISE EXCEPTION USING ERRCODE='55000',
                  MESSAGE=format('Refusing ordinary-owner ACL convergence: partial retired state (was installed %s, roles %s/%s).',
                    was_installed,role_count,cardinality(target_roles));
              END IF;
              IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname='nexa_erp_owner') THEN
                IF NOT was_installed AND role_count=0 THEN
                  RETURN;
                END IF;
                RAISE EXCEPTION USING ERRCODE='55000',
                  MESSAGE='Refusing ordinary-owner ACL convergence: nexa_erp_owner is missing.';
              END IF;

              IF was_installed THEN
                FOREACH role_name IN ARRAY target_roles LOOP
                  EXECUTE format('REASSIGN OWNED BY %I TO nexa_erp_owner',role_name);
                END LOOP;
              END IF;

              GRANT USAGE,CREATE ON SCHEMA advance TO nexa_erp_owner;
              GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA advance TO nexa_erp_owner;
              GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA advance TO nexa_erp_owner;
              GRANT ALL PRIVILEGES ON ALL FUNCTIONS IN SCHEMA advance TO nexa_erp_owner;
            END $converge$;
            """;
    }
}
